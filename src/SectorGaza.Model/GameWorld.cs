namespace SectorGaza.Model;

public sealed class GameWorld
{
    public GameWorld(Room room, Player player)
    {
        Room = room;
        Player = player;
    }

    public Room Room { get; }

    public Player Player { get; }

    public static GameWorld CreateDefault()
    {
        var room = Room.CreateDefault();
        var player = new Player(new IntRectangle(80, 80, 28, 28));
        return new GameWorld(room, player);
    }

    public void Update(InputState inputState)
    {
        var axes = inputState.GetMovementAxes();
        Player.Update(axes.Horizontal, axes.Vertical, Room);
    }
}
