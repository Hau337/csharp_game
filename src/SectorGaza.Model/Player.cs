namespace SectorGaza.Model;

public sealed class Player
{
    public const int DefaultSpeed = 6;
    public const int MaxHealth = 115;
    public const int AttackDamage = 32;
    public const int AttackRange = 72;

    private const double RotationStepDegrees = 14;
    private const int AttackCooldownTicks = 14;
    private const int AttackFlashTicks = 5;
    private const int DamageCooldownTicks = 30;

    private int attackCooldown;
    private int attackFlash;
    private int damageCooldown;

    public Player(IntRectangle bounds)
    {
        Bounds = bounds;
        CurrentHealth = MaxHealth;
    }

    public IntRectangle Bounds { get; private set; }

    public int Speed { get; } = DefaultSpeed;

    public int CurrentHealth { get; private set; }

    public bool IsAlive => CurrentHealth > 0;

    public bool IsAttackFlashVisible => attackFlash > 0;

    public double FacingAngleDegrees { get; private set; }

    public void Tick()
    {
        if (attackCooldown > 0)
        {
            attackCooldown--;
        }

        if (attackFlash > 0)
        {
            attackFlash--;
        }

        if (damageCooldown > 0)
        {
            damageCooldown--;
        }
    }

    public void UpdateMovement(int horizontalAxis, int verticalAxis, Room room)
    {
        UpdateFacing(horizontalAxis, verticalAxis);
        if (!IsAlive || (horizontalAxis == 0 && verticalAxis == 0))
        {
            return;
        }

        var movement = CalculateMovement(horizontalAxis, verticalAxis);
        TryMove(movement.dx, 0, room.Walls);
        TryMove(0, movement.dy, room.Walls);
    }

    public bool TryAttack(IReadOnlyList<Enemy> enemies)
    {
        if (!IsAlive || attackCooldown > 0)
        {
            return false;
        }

        attackCooldown = AttackCooldownTicks;
        attackFlash = AttackFlashTicks;

        Enemy? target = null;
        var minDistance = double.MaxValue;

        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive || !IsEnemyInAttackRange(enemy, out var distance))
            {
                continue;
            }

            if (distance < minDistance)
            {
                minDistance = distance;
                target = enemy;
            }
        }

        if (target is null)
        {
            return false;
        }

        target.TakeDamage(AttackDamage);
        return true;
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive || damageCooldown > 0 || damage <= 0)
        {
            return;
        }

        damageCooldown = DamageCooldownTicks;
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
    }

    public bool Heal(int amount)
    {
        if (!IsAlive || amount <= 0 || CurrentHealth >= MaxHealth)
        {
            return false;
        }

        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        return true;
    }

    public void TeleportTo(IntRectangle bounds)
    {
        Bounds = bounds;
    }

    private bool IsEnemyInAttackRange(Enemy enemy, out double distance)
    {
        var selfCenter = GetCenter(Bounds);
        var enemyCenter = GetCenter(enemy.Bounds);
        var dx = enemyCenter.X - selfCenter.X;
        var dy = enemyCenter.Y - selfCenter.Y;
        distance = Math.Sqrt((dx * dx) + (dy * dy));
        if (distance > AttackRange)
        {
            return false;
        }

        var facingRadians = FacingAngleDegrees * (Math.PI / 180.0);
        var forwardX = Math.Cos(facingRadians);
        var forwardY = Math.Sin(facingRadians);
        var dot = (dx * forwardX) + (dy * forwardY);
        return dot >= -2;
    }

    private static (double X, double Y) GetCenter(IntRectangle rectangle)
    {
        return
        (
            rectangle.X + (rectangle.Width / 2.0),
            rectangle.Y + (rectangle.Height / 2.0)
        );
    }

    private (int dx, int dy) CalculateMovement(int horizontalAxis, int verticalAxis)
    {
        var length = Math.Sqrt((horizontalAxis * horizontalAxis) + (verticalAxis * verticalAxis));
        if (length < 0.001)
        {
            return (0, 0);
        }

        var dx = (int)Math.Round((horizontalAxis / length) * Speed);
        var dy = (int)Math.Round((verticalAxis / length) * Speed);
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

        FacingAngleDegrees = NormalizeAngle(FacingAngleDegrees + (Math.Sign(delta) * RotationStepDegrees));
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
