namespace SectorGaza.Model;

public sealed class Player
{
    public const int DefaultSpeed = 6;
    private const double RotationStepDegrees = 14;

    public Player(IntRectangle bounds)
    {
        Bounds = bounds;
    }

    public IntRectangle Bounds { get; private set; }

    public int Speed { get; } = DefaultSpeed;

    public double FacingAngleDegrees { get; private set; }

    public void Update(int horizontalAxis, int verticalAxis, Room room)
    {
        UpdateFacing(horizontalAxis, verticalAxis);
        if (horizontalAxis == 0 && verticalAxis == 0)
        {
            return;
        }

        var movement = CalculateMovement(horizontalAxis, verticalAxis);
        TryMove(movement.dx, 0, room.Walls);
        TryMove(0, movement.dy, room.Walls);
    }

    private static (int dx, int dy) CalculateMovement(int horizontalAxis, int verticalAxis)
    {
        var length = Math.Sqrt((horizontalAxis * horizontalAxis) + (verticalAxis * verticalAxis));
        if (length < 0.001)
        {
            return (0, 0);
        }

        var dx = (int)Math.Round((horizontalAxis / length) * DefaultSpeed);
        var dy = (int)Math.Round((verticalAxis / length) * DefaultSpeed);
        return (dx, dy);
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

    private void UpdateFacing(int horizontalAxis, int verticalAxis)
    {
        if (horizontalAxis == 0 && verticalAxis == 0)
        {
            return;
        }

        var targetAngle = NormalizeAngle(Math.Atan2(verticalAxis, horizontalAxis) * (180.0 / Math.PI));

        var delta = NormalizeDelta(targetAngle - FacingAngleDegrees);
        if (Math.Abs(delta) <= RotationStepDegrees)
        {
            FacingAngleDegrees = targetAngle;
            return;
        }

        FacingAngleDegrees = NormalizeAngle(FacingAngleDegrees + Math.Sign(delta) * RotationStepDegrees);
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

    private static double NormalizeDelta(double angle)
    {
        while (angle > 180)
        {
            angle -= 360;
        }

        while (angle < -180)
        {
            angle += 360;
        }

        return angle;
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
