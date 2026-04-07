namespace SectorGaza.Model;

public sealed class GameWorld
{
    private const int TotalEnemiesCount = 1;

    public GameWorld(Room room, Player player, Enemy enemy)
    {
        Room = room;
        Player = player;
        Enemy = enemy;
    }

    public Room Room { get; }

    public Player Player { get; }

    public Enemy Enemy { get; }

    public bool IsGameOver => !Player.IsAlive;

    public int TotalEnemies => TotalEnemiesCount;

    public int AliveEnemies => Enemy.IsAlive ? 1 : 0;

    public static GameWorld CreateDefault()
    {
        var room = Room.CreateDefault();
        var player = new Player(new IntRectangle(80, 80, 28, 28));
        var enemy = new Enemy(new IntRectangle(760, 320, 30, 30));
        return new GameWorld(room, player, enemy);
    }

    public void Update(InputState inputState)
    {
        Player.Tick();

        if (IsGameOver)
        {
            return;
        }

        var axes = inputState.GetMovementAxes();
        Player.UpdateMovement(axes.Horizontal, axes.Vertical, Room);

        if (inputState.ConsumeAttackRequest())
        {
            Player.TryAttack(Enemy);
        }

        Enemy.Update(Player, Room);
        if (Enemy.IsTouching(Player))
        {
            Player.TakeDamage(Enemy.ContactDamage);
        }
    }
}
