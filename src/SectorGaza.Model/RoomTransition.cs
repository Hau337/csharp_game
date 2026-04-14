namespace SectorGaza.Model;

public sealed class RoomTransition
{
    public RoomTransition(
        IntRectangle triggerBounds,
        int targetRoomIndex,
        IntRectangle targetPlayerBounds,
        Direction direction)
    {
        TriggerBounds = triggerBounds;
        TargetRoomIndex = targetRoomIndex;
        TargetPlayerBounds = targetPlayerBounds;
        Direction = direction;
    }

    public IntRectangle TriggerBounds { get; }

    public int TargetRoomIndex { get; }

    public IntRectangle TargetPlayerBounds { get; }

    public Direction Direction { get; }
}
