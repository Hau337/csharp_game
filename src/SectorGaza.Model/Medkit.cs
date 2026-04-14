namespace SectorGaza.Model;

public sealed class Medkit
{
    public const int DefaultHealAmount = 35;

    public Medkit(IntRectangle bounds, int healAmount = DefaultHealAmount)
    {
        Bounds = bounds;
        HealAmount = healAmount;
    }

    public IntRectangle Bounds { get; }

    public int HealAmount { get; }

    public bool IsCollected { get; private set; }

    public void TryCollect(Player player)
    {
        if (IsCollected || !player.IsAlive || !Bounds.IntersectsWith(player.Bounds))
        {
            return;
        }

        if (player.Heal(HealAmount))
        {
            IsCollected = true;
        }
    }
}
