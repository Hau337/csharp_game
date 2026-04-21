namespace SectorGaza.Model;

public sealed class LevelRoom
{
    public LevelRoom(
        string name,
        Room room,
        IReadOnlyList<Enemy> enemies,
        IReadOnlyList<Medkit> medkits,
        IReadOnlyList<RoomTransition> transitions,
        IReadOnlyList<StoryNote>? notes = null,
        KeyCard? keyCard = null,
        FinalDoor? finalDoor = null)
    {
        Name = name;
        Room = room;
        Enemies = PlaceEnemies(room, enemies);
        Medkits = medkits;
        Transitions = transitions;
        Notes = notes ?? Array.Empty<StoryNote>();
        KeyCard = keyCard;
        FinalDoor = finalDoor;
    }

    public string Name { get; }

    public Room Room { get; }

    public IReadOnlyList<Enemy> Enemies { get; }

    public IReadOnlyList<Medkit> Medkits { get; }

    public IReadOnlyList<RoomTransition> Transitions { get; }

    public IReadOnlyList<StoryNote> Notes { get; }

    public KeyCard? KeyCard { get; }

    public FinalDoor? FinalDoor { get; }

    private static IReadOnlyList<Enemy> PlaceEnemies(Room room, IReadOnlyList<Enemy> enemies)
    {
        var placedBounds = new List<IntRectangle>();
        var placedEnemies = new List<Enemy>(enemies.Count);

        foreach (var enemy in enemies)
        {
            var freeBounds = FindNearestFreeBounds(enemy.Bounds, room, placedBounds);
            placedBounds.Add(freeBounds);
            placedEnemies.Add(new Enemy(freeBounds, enemy.Kind));
        }

        return placedEnemies;
    }

    private static IntRectangle FindNearestFreeBounds(
        IntRectangle preferredBounds,
        Room room,
        IReadOnlyList<IntRectangle> occupiedBounds)
    {
        if (IsPlacementFree(preferredBounds, room, occupiedBounds))
        {
            return preferredBounds;
        }

        const int step = 12;
        const int maxRadius = 420;
        for (var radius = step; radius <= maxRadius; radius += step)
        {
            for (var y = -radius; y <= radius; y += step)
            {
                for (var x = -radius; x <= radius; x += step)
                {
                    if (Math.Abs(x) != radius && Math.Abs(y) != radius)
                    {
                        continue;
                    }

                    var candidate = preferredBounds.Offset(x, y);
                    if (IsPlacementFree(candidate, room, occupiedBounds))
                    {
                        return candidate;
                    }
                }
            }
        }

        return preferredBounds;
    }

    private static bool IsPlacementFree(
        IntRectangle bounds,
        Room room,
        IReadOnlyList<IntRectangle> occupiedBounds)
    {
        if (!IsInsideRoom(bounds, room))
        {
            return false;
        }

        foreach (var wall in room.Walls)
        {
            if (bounds.IntersectsWith(wall.Bounds))
            {
                return false;
            }
        }

        foreach (var occupied in occupiedBounds)
        {
            if (bounds.IntersectsWith(occupied))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsInsideRoom(IntRectangle bounds, Room room)
    {
        return bounds.X >= 0
            && bounds.Y >= 0
            && bounds.Right <= room.Width
            && bounds.Bottom <= room.Height;
    }
}
