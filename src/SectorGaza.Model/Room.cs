namespace SectorGaza.Model;

public sealed class Room
{
    public Room(int width, int height, IReadOnlyList<Wall> walls)
    {
        Width = width;
        Height = height;
        Walls = walls;
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<Wall> Walls { get; }

    public static Room CreateDefault()
    {
        var walls = new List<Wall>
        {
            new(new IntRectangle(0, 0, 960, 32)),
            new(new IntRectangle(0, 0, 32, 640)),
            new(new IntRectangle(0, 608, 960, 32)),
            new(new IntRectangle(928, 0, 32, 640)),
            new(new IntRectangle(192, 128, 32, 320)),
            new(new IntRectangle(224, 128, 224, 32)),
            new(new IntRectangle(544, 192, 32, 288)),
            new(new IntRectangle(544, 192, 192, 32)),
            new(new IntRectangle(320, 480, 256, 32))
        };

        return new Room(960, 640, walls);
    }
}
