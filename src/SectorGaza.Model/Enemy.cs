namespace SectorGaza.Model;

public sealed class Enemy
{
    public const int NormalSpeed = 3;
    public const int NormalMaxHealth = 90;
    public const int NormalContactDamage = 9;
    public const int FastSpeed = 4;
    public const int FastMaxHealth = 55;
    public const int FastContactDamage = 7;
    private const double AggroRange = 520;
    private const int PathCellSize = 48;
    private const int PathRebuildIntervalTicks = 16;
    private const int PathMaxSteps = 72;

    private readonly int speed;
    private readonly List<(int X, int Y)> pathCells = [];
    private int pathCellIndex;
    private int pathRebuildCooldown;
    private (int X, int Y) lastTargetCell;
    private bool hasTargetCell;

    public Enemy(IntRectangle bounds, EnemyKind kind = EnemyKind.Normal)
    {
        Bounds = bounds;
        Kind = kind;
        if (kind == EnemyKind.Fast)
        {
            speed = FastSpeed;
            ContactDamage = FastContactDamage;
            CurrentHealth = FastMaxHealth;
            return;
        }

        speed = NormalSpeed;
        ContactDamage = NormalContactDamage;
        CurrentHealth = NormalMaxHealth;
    }

    public IntRectangle Bounds { get; private set; }

    public EnemyKind Kind { get; }

    public int CurrentHealth { get; private set; }

    public int ContactDamage { get; }

    public bool IsAlive => CurrentHealth > 0;

    public double FacingAngleDegrees { get; private set; }

    public void Update(Player player, Room room)
    {
        if (!IsAlive || !player.IsAlive)
        {
            return;
        }

        var selfCenter = GetCenter(Bounds);
        var playerCenter = GetCenter(player.Bounds);
        var dxToPlayer = playerCenter.X - selfCenter.X;
        var dyToPlayer = playerCenter.Y - selfCenter.Y;
        var length = Math.Sqrt((dxToPlayer * dxToPlayer) + (dyToPlayer * dyToPlayer));
        if (length < 0.01)
        {
            ClearPath();
            return;
        }

        if (length > AggroRange)
        {
            ClearPath();
            return;
        }

        FacingAngleDegrees = NormalizeAngle(Math.Atan2(dyToPlayer, dxToPlayer) * (180.0 / Math.PI));
        if (pathRebuildCooldown > 0)
        {
            pathRebuildCooldown--;
        }

        var selfCell = ToCell(selfCenter, room);
        var targetCell = ToCell(playerCenter, room);
        if (ShouldRebuildPath(selfCell, targetCell))
        {
            BuildPath(selfCell, targetCell, room);
        }

        var moveTarget = GetMoveTarget(selfCenter, playerCenter, room);
        if (!TryMoveTowards(moveTarget, room.Walls))
        {
            pathRebuildCooldown = 0;
        }
    }

    public bool IsTouching(Player player)
    {
        return IsAlive && player.IsAlive && Bounds.IntersectsWith(player.Bounds);
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive || damage <= 0)
        {
            return;
        }

        CurrentHealth = Math.Max(0, CurrentHealth - damage);
    }

    private static (double X, double Y) GetCenter(IntRectangle rectangle)
    {
        return
        (
            rectangle.X + (rectangle.Width / 2.0),
            rectangle.Y + (rectangle.Height / 2.0)
        );
    }

    private bool ShouldRebuildPath((int X, int Y) selfCell, (int X, int Y) targetCell)
    {
        if (pathCells.Count == 0 || pathCellIndex >= pathCells.Count)
        {
            return true;
        }

        if (pathRebuildCooldown <= 0)
        {
            return true;
        }

        if (!hasTargetCell || lastTargetCell != targetCell)
        {
            return true;
        }

        if (pathCellIndex < pathCells.Count && pathCells[pathCellIndex] == selfCell)
        {
            pathCellIndex++;
            return pathCellIndex >= pathCells.Count;
        }

        return false;
    }

    private void BuildPath((int X, int Y) startCell, (int X, int Y) targetCell, Room room)
    {
        pathRebuildCooldown = PathRebuildIntervalTicks;
        hasTargetCell = true;
        lastTargetCell = targetCell;
        pathCells.Clear();
        pathCellIndex = 0;

        var columns = GetGridColumns(room);
        var rows = GetGridRows(room);
        var walkableGrid = BuildWalkableGrid(room, columns, rows);
        startCell = ClampCell(startCell, columns, rows);
        targetCell = ClampCell(targetCell, columns, rows);

        if (!walkableGrid[startCell.X, startCell.Y])
        {
            return;
        }

        if (!walkableGrid[targetCell.X, targetCell.Y]
            && !TryFindNearestWalkable(targetCell, walkableGrid, out targetCell))
        {
            return;
        }

        var visited = new bool[columns, rows];
        var parentX = new int[columns, rows];
        var parentY = new int[columns, rows];
        for (var x = 0; x < columns; x++)
        {
            for (var y = 0; y < rows; y++)
            {
                parentX[x, y] = -1;
                parentY[x, y] = -1;
            }
        }

        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(startCell);
        visited[startCell.X, startCell.Y] = true;

        var foundPath = false;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == targetCell)
            {
                foundPath = true;
                break;
            }

            foreach (var neighbor in GetNeighbors(current, columns, rows))
            {
                if (visited[neighbor.X, neighbor.Y] || !walkableGrid[neighbor.X, neighbor.Y])
                {
                    continue;
                }

                visited[neighbor.X, neighbor.Y] = true;
                parentX[neighbor.X, neighbor.Y] = current.X;
                parentY[neighbor.X, neighbor.Y] = current.Y;
                queue.Enqueue(neighbor);
            }
        }

        if (!foundPath)
        {
            return;
        }

        var reversedPath = new List<(int X, int Y)>();
        var node = targetCell;
        while (node != startCell)
        {
            reversedPath.Add(node);
            var previousX = parentX[node.X, node.Y];
            var previousY = parentY[node.X, node.Y];
            if (previousX < 0 || previousY < 0)
            {
                reversedPath.Clear();
                break;
            }

            node = (previousX, previousY);
        }

        reversedPath.Reverse();
        if (reversedPath.Count > PathMaxSteps)
        {
            reversedPath.RemoveRange(PathMaxSteps, reversedPath.Count - PathMaxSteps);
        }

        pathCells.AddRange(reversedPath);
    }

    private bool[,] BuildWalkableGrid(Room room, int columns, int rows)
    {
        var grid = new bool[columns, rows];
        for (var x = 0; x < columns; x++)
        {
            for (var y = 0; y < rows; y++)
            {
                var cellBounds = BuildBoundsForCell((x, y), room);
                grid[x, y] = IsInsideRoom(cellBounds, room) && !CollidesWithWall(cellBounds, room.Walls);
            }
        }

        return grid;
    }

    private bool TryFindNearestWalkable((int X, int Y) preferredCell, bool[,] walkableGrid, out (int X, int Y) walkableCell)
    {
        walkableCell = preferredCell;
        var columns = walkableGrid.GetLength(0);
        var rows = walkableGrid.GetLength(1);
        var maxRadius = Math.Max(columns, rows);

        for (var radius = 1; radius <= maxRadius; radius++)
        {
            for (var y = preferredCell.Y - radius; y <= preferredCell.Y + radius; y++)
            {
                for (var x = preferredCell.X - radius; x <= preferredCell.X + radius; x++)
                {
                    if (Math.Abs(x - preferredCell.X) != radius
                        && Math.Abs(y - preferredCell.Y) != radius)
                    {
                        continue;
                    }

                    if (x < 0 || y < 0 || x >= columns || y >= rows)
                    {
                        continue;
                    }

                    if (!walkableGrid[x, y])
                    {
                        continue;
                    }

                    walkableCell = (x, y);
                    return true;
                }
            }
        }

        return false;
    }

    private (double X, double Y) GetMoveTarget((double X, double Y) selfCenter, (double X, double Y) playerCenter, Room room)
    {
        while (pathCellIndex < pathCells.Count)
        {
            var waypointCenter = GetCellCenter(pathCells[pathCellIndex], room);
            var waypointDistanceX = waypointCenter.X - selfCenter.X;
            var waypointDistanceY = waypointCenter.Y - selfCenter.Y;
            var waypointDistance = Math.Sqrt((waypointDistanceX * waypointDistanceX) + (waypointDistanceY * waypointDistanceY));
            if (waypointDistance <= speed + 2)
            {
                pathCellIndex++;
                continue;
            }

            return waypointCenter;
        }

        return playerCenter;
    }

    private bool TryMoveTowards((double X, double Y) target, IReadOnlyList<Wall> walls)
    {
        var center = GetCenter(Bounds);
        var dx = target.X - center.X;
        var dy = target.Y - center.Y;
        var length = Math.Sqrt((dx * dx) + (dy * dy));
        if (length < 0.01)
        {
            return false;
        }

        var moveX = (int)Math.Round((dx / length) * speed);
        var moveY = (int)Math.Round((dy / length) * speed);
        if (moveX == 0 && moveY == 0)
        {
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                moveX = Math.Sign(dx);
            }
            else
            {
                moveY = Math.Sign(dy);
            }
        }

        return TryMove(moveX, moveY, walls);
    }

    private bool TryMove(int dx, int dy, IReadOnlyList<Wall> walls)
    {
        var moved = false;
        moved |= TryMoveAxis(dx, 0, walls);
        moved |= TryMoveAxis(0, dy, walls);
        return moved;
    }

    private bool TryMoveAxis(int dx, int dy, IReadOnlyList<Wall> walls)
    {
        if (dx == 0 && dy == 0)
        {
            return false;
        }

        var movedBounds = Bounds.Offset(dx, dy);
        if (CollidesWithWall(movedBounds, walls))
        {
            return false;
        }

        Bounds = movedBounds;
        return true;
    }

    private IntRectangle BuildBoundsForCell((int X, int Y) cell, Room room)
    {
        var centerX = (cell.X * PathCellSize) + (PathCellSize / 2.0);
        var centerY = (cell.Y * PathCellSize) + (PathCellSize / 2.0);
        var x = (int)Math.Round(centerX - (Bounds.Width / 2.0));
        var y = (int)Math.Round(centerY - (Bounds.Height / 2.0));
        x = Math.Clamp(x, 0, room.Width - Bounds.Width);
        y = Math.Clamp(y, 0, room.Height - Bounds.Height);
        return new IntRectangle(x, y, Bounds.Width, Bounds.Height);
    }

    private static bool IsInsideRoom(IntRectangle bounds, Room room)
    {
        return bounds.X >= 0
            && bounds.Y >= 0
            && bounds.Right <= room.Width
            && bounds.Bottom <= room.Height;
    }

    private static int GetGridColumns(Room room)
    {
        return Math.Max(1, (int)Math.Ceiling(room.Width / (double)PathCellSize));
    }

    private static int GetGridRows(Room room)
    {
        return Math.Max(1, (int)Math.Ceiling(room.Height / (double)PathCellSize));
    }

    private static (int X, int Y) ToCell((double X, double Y) point, Room room)
    {
        var columns = GetGridColumns(room);
        var rows = GetGridRows(room);
        var cellX = Math.Clamp((int)(point.X / PathCellSize), 0, columns - 1);
        var cellY = Math.Clamp((int)(point.Y / PathCellSize), 0, rows - 1);
        return (cellX, cellY);
    }

    private static (int X, int Y) ClampCell((int X, int Y) cell, int columns, int rows)
    {
        return
        (
            Math.Clamp(cell.X, 0, columns - 1),
            Math.Clamp(cell.Y, 0, rows - 1)
        );
    }

    private static (double X, double Y) GetCellCenter((int X, int Y) cell, Room room)
    {
        var x = (cell.X * PathCellSize) + (PathCellSize / 2.0);
        var y = (cell.Y * PathCellSize) + (PathCellSize / 2.0);
        return
        (
            Math.Clamp(x, 0, room.Width),
            Math.Clamp(y, 0, room.Height)
        );
    }

    private static IEnumerable<(int X, int Y)> GetNeighbors((int X, int Y) cell, int columns, int rows)
    {
        var candidates = new (int X, int Y)[]
        {
            (cell.X + 1, cell.Y),
            (cell.X - 1, cell.Y),
            (cell.X, cell.Y + 1),
            (cell.X, cell.Y - 1)
        };

        foreach (var candidate in candidates)
        {
            if (candidate.X < 0 || candidate.Y < 0 || candidate.X >= columns || candidate.Y >= rows)
            {
                continue;
            }

            yield return candidate;
        }
    }

    private void ClearPath()
    {
        pathCells.Clear();
        pathCellIndex = 0;
        pathRebuildCooldown = 0;
        hasTargetCell = false;
    }

    private static bool CollidesWithWall(IntRectangle bounds, IReadOnlyList<Wall> walls)
    {
        foreach (var wall in walls)
        {
            if (bounds.IntersectsWith(wall.Bounds))
            {
                return true;
            }
        }

        return false;
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle >= 360)
        {
            angle -= 360;
        }

        while (angle < 0)
        {
            angle += 360;
        }

        return angle;
    }
}
