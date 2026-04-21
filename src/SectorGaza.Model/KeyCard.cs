namespace SectorGaza.Model;

public sealed class KeyCard
{
    public KeyCard(IntRectangle bounds)
    {
        Bounds = bounds;
    }

    public IntRectangle Bounds { get; }

    public bool IsCollected { get; private set; }

    public bool TryCollect(Player player)
    {
        if (IsCollected || !player.IsAlive || !Bounds.IntersectsWith(player.Bounds))
        {
            return false;
        }

        IsCollected = true;
        return true;
    }
}
