namespace SectorGaza.Model;

public sealed class Wall
{
    public Wall(IntRectangle bounds)
    {
        Bounds = bounds;
    }

    public IntRectangle Bounds { get; }
}
