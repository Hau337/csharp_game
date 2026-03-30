using SectorGaza.Model;

namespace SectorGaza.WinForms;

public sealed class GameController
{
    private readonly InputState inputState = new();

    public GameController(GameWorld gameWorld)
    {
        GameWorld = gameWorld;
    }

    public GameWorld GameWorld { get; }

    public void Update()
    {
        GameWorld.Update(inputState);
    }

    public void HandleKeyDown(Keys key)
    {
        switch (key)
        {
            case Keys.W:
                inputState.MoveUp = true;
                break;
            case Keys.S:
                inputState.MoveDown = true;
                break;
            case Keys.A:
                inputState.MoveLeft = true;
                break;
            case Keys.D:
                inputState.MoveRight = true;
                break;
        }
    }

    public void HandleKeyUp(Keys key)
    {
        switch (key)
        {
            case Keys.W:
                inputState.MoveUp = false;
                break;
            case Keys.S:
                inputState.MoveDown = false;
                break;
            case Keys.A:
                inputState.MoveLeft = false;
                break;
            case Keys.D:
                inputState.MoveRight = false;
                break;
        }
    }
}
