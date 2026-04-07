namespace SectorGaza.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var controller = new GameController();
        var renderer = new GameRenderer();

        Application.Run(new GameForm(controller, renderer));
    }
}
