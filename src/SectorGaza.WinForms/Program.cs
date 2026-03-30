using SectorGaza.Model;

namespace SectorGaza.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var gameWorld = GameWorld.CreateDefault();
        var controller = new GameController(gameWorld);
        var renderer = new GameRenderer();

        Application.Run(new GameForm(controller, renderer));
    }
}
