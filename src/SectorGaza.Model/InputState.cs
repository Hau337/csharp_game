namespace SectorGaza.Model;

public sealed class InputState
{
    public bool MoveUp { get; set; }

    public bool MoveDown { get; set; }

    public bool MoveLeft { get; set; }

    public bool MoveRight { get; set; }

    private bool attackRequested;
    private bool interactRequested;

    public (int Horizontal, int Vertical) GetMovementAxes()
    {
        var horizontal = (MoveRight ? 1 : 0) - (MoveLeft ? 1 : 0);
        var vertical = (MoveDown ? 1 : 0) - (MoveUp ? 1 : 0);
        return (horizontal, vertical);
    }

    public void RequestAttack()
    {
        attackRequested = true;
    }

    public bool ConsumeAttackRequest()
    {
        var requested = attackRequested;
        attackRequested = false;
        return requested;
    }

    public void RequestInteract()
    {
        interactRequested = true;
    }

    public bool ConsumeInteractRequest()
    {
        var requested = interactRequested;
        interactRequested = false;
        return requested;
    }

    public void Reset()
    {
        MoveUp = false;
        MoveDown = false;
        MoveLeft = false;
        MoveRight = false;
        attackRequested = false;
        interactRequested = false;
    }
}
