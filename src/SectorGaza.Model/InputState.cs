namespace SectorGaza.Model;

public sealed class InputState
{
    public bool MoveUp { get; set; }

    public bool MoveDown { get; set; }

    public bool MoveLeft { get; set; }

    public bool MoveRight { get; set; }

    public (int Horizontal, int Vertical) GetMovementAxes()
    {
        var horizontal = (MoveRight ? 1 : 0) - (MoveLeft ? 1 : 0);
        var vertical = (MoveDown ? 1 : 0) - (MoveUp ? 1 : 0);
        return (horizontal, vertical);
    }
}
