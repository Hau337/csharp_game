namespace SectorGaza.Model;

public sealed class StoryNote
{
    public StoryNote(IntRectangle bounds, string title, string text)
    {
        Bounds = bounds;
        Title = title;
        Text = text;
    }

    public IntRectangle Bounds { get; }

    public string Title { get; }

    public string Text { get; }

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
