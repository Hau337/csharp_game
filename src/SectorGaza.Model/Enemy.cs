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

    private readonly int speed;

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
            return;
        }

        if (length > AggroRange)
        {
            return;
        }

        FacingAngleDegrees = NormalizeAngle(Math.Atan2(dyToPlayer, dxToPlayer) * (180.0 / Math.PI));

        var moveX = (int)Math.Round((dxToPlayer / length) * speed);
        var moveY = (int)Math.Round((dyToPlayer / length) * speed);
        TryMove(moveX, 0, room.Walls);
        TryMove(0, moveY, room.Walls);
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

    private void TryMove(int dx, int dy, IReadOnlyList<Wall> walls)
    {
        if (dx == 0 && dy == 0)
        {
            return;
        }

        var movedBounds = Bounds.Offset(dx, dy);
        if (CollidesWithWall(movedBounds, walls))
        {
            return;
        }

        Bounds = movedBounds;
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
