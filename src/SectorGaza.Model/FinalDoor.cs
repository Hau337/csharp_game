namespace SectorGaza.Model;

public sealed class FinalDoor
{
    public FinalDoor(IntRectangle bounds)
    {
        Bounds = bounds;
    }

    public IntRectangle Bounds { get; }

    public bool IsOpened { get; private set; }

    public void Open()
    {
        IsOpened = true;
    }
}
