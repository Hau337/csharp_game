namespace SectorGaza.Model;

public readonly record struct IntRectangle(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;

    public bool IntersectsWith(IntRectangle other)
    {
        return X < other.Right
            && Right > other.X
            && Y < other.Bottom
            && Bottom > other.Y;
    }

    public IntRectangle Offset(int dx, int dy)
    {
        return this with
        {
            X = X + dx,
            Y = Y + dy
        };
    }
}
