using SectorGaza.Model;
using Xunit;

namespace SectorGaza.Model.Tests;

public sealed class ModelTests
{
    [Fact]
    public void PlayerAttack_HitsEnemyInFront_AndSkipsBehindEnemy()
    {
        var room = new Room(400, 300, Array.Empty<Wall>());
        var player = new Player(new IntRectangle(100, 100, 28, 28));
        var enemyInFront = new Enemy(new IntRectangle(160, 100, 30, 30), EnemyKind.Normal);
        var enemyBehind = new Enemy(new IntRectangle(40, 100, 30, 30), EnemyKind.Normal);
        var enemies = new[] { enemyInFront, enemyBehind };

        player.UpdateMovement(1, 0, room);
        var attacked = player.TryAttack(enemies);

        Assert.True(attacked);
        Assert.Equal(Enemy.NormalMaxHealth - Player.AttackDamage, enemyInFront.CurrentHealth);
        Assert.Equal(Enemy.NormalMaxHealth, enemyBehind.CurrentHealth);
    }

    [Fact]
    public void Medkit_IsCollectedOnlyWhenPlayerNeedsHealing()
    {
        var player = new Player(new IntRectangle(100, 100, 28, 28));
        var medkit = new Medkit(new IntRectangle(100, 100, 24, 24));

        medkit.TryCollect(player);
        Assert.False(medkit.IsCollected);

        player.TakeDamage(20);
        medkit.TryCollect(player);

        Assert.True(medkit.IsCollected);
        Assert.Equal(Player.MaxHealth, player.CurrentHealth);
    }

    [Fact]
    public void GameWorld_DoesNotMoveForward_WhenCurrentRoomIsNotCleared()
    {
        var world = GameWorld.CreateDefault();
        var roomBefore = world.CurrentRoomNumber;
        var forwardTransition = GetForwardTransition(world);

        TeleportPlayerIntoBounds(world, forwardTransition.TriggerBounds);
        world.Update(new InputState());

        Assert.Equal(roomBefore, world.CurrentRoomNumber);
        Assert.Contains("зачистки", world.ActiveStoryMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GameWorld_AllowsVictory_WhenKeyCardCollectedAndFinalRoomCleared()
    {
        var world = GameWorld.CreateDefault();

        ClearCurrentRoom(world);
        MoveToNextRoom(world);
        Assert.Equal(2, world.CurrentRoomNumber);

        Assert.NotNull(world.KeyCard);
        TeleportPlayerIntoBounds(world, world.KeyCard!.Bounds);
        var interactInput = new InputState();
        interactInput.RequestInteract();
        world.Update(interactInput);
        Assert.True(world.HasKeyCard);

        ClearCurrentRoom(world);
        MoveToNextRoom(world);
        Assert.Equal(3, world.CurrentRoomNumber);

        ClearCurrentRoom(world);
        Assert.NotNull(world.FinalDoor);
        TeleportPlayerIntoBounds(world, world.FinalDoor!.Bounds);
        interactInput = new InputState();
        interactInput.RequestInteract();
        world.Update(interactInput);

        Assert.True(world.IsVictory);
    }

    [Fact]
    public void GameWorld_BlocksFinalDoorWithoutKeyCard()
    {
        var world = GameWorld.CreateDefault();

        ClearCurrentRoom(world);
        MoveToNextRoom(world);
        Assert.Equal(2, world.CurrentRoomNumber);

        ClearCurrentRoom(world);
        MoveToNextRoom(world);
        Assert.Equal(3, world.CurrentRoomNumber);

        ClearCurrentRoom(world);
        Assert.NotNull(world.FinalDoor);
        TeleportPlayerIntoBounds(world, world.FinalDoor!.Bounds);
        var interactInput = new InputState();
        interactInput.RequestInteract();
        world.Update(interactInput);

        Assert.False(world.IsVictory);
        Assert.Contains("ключ", world.ActiveStoryMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Enemy_PathfindsAroundWall_WhenChasingPlayer()
    {
        var walls = new List<Wall>
        {
            new(new IntRectangle(0, 0, 480, 20)),
            new(new IntRectangle(0, 0, 20, 320)),
            new(new IntRectangle(0, 300, 480, 20)),
            new(new IntRectangle(460, 0, 20, 320)),
            new(new IntRectangle(220, 20, 20, 180))
        };
        var room = new Room(480, 320, walls);
        var player = new Player(new IntRectangle(360, 80, 28, 28));
        var enemy = new Enemy(new IntRectangle(80, 80, 30, 30), EnemyKind.Normal);

        for (var i = 0; i < 320; i++)
        {
            enemy.Update(player, room);
            Assert.DoesNotContain(room.Walls, wall => enemy.Bounds.IntersectsWith(wall.Bounds));
        }

        Assert.True(enemy.Bounds.X > 170, $"Enemy did not approach the obstacle. Current X: {enemy.Bounds.X}");
        Assert.True(
            Math.Abs(enemy.Bounds.Y - 80) > 20,
            $"Enemy did not try to обходить стену по вертикали. Current Y: {enemy.Bounds.Y}");
    }

    private static void ClearCurrentRoom(GameWorld world)
    {
        foreach (var enemy in world.Enemies)
        {
            enemy.TakeDamage(10_000);
        }
    }

    private static RoomTransition GetForwardTransition(GameWorld world)
    {
        var currentRoomIndex = world.CurrentRoomNumber - 1;
        return world.Transitions.First(transition => transition.TargetRoomIndex > currentRoomIndex);
    }

    private static void MoveToNextRoom(GameWorld world)
    {
        var roomBefore = world.CurrentRoomNumber;
        for (var tick = 0; tick < 60; tick++)
        {
            var transition = GetForwardTransition(world);
            TeleportPlayerIntoBounds(world, transition.TriggerBounds);
            world.Update(new InputState());
            if (world.CurrentRoomNumber > roomBefore)
            {
                return;
            }
        }

        Assert.Fail("Failed to move to the next room within 60 ticks.");
    }

    private static void TeleportPlayerIntoBounds(GameWorld world, IntRectangle targetBounds)
    {
        var playerBounds = world.Player.Bounds;
        var x = targetBounds.X + Math.Max(0, (targetBounds.Width - playerBounds.Width) / 2);
        var y = targetBounds.Y + Math.Max(0, (targetBounds.Height - playerBounds.Height) / 2);
        world.Player.TeleportTo(new IntRectangle(x, y, playerBounds.Width, playerBounds.Height));
    }
}
