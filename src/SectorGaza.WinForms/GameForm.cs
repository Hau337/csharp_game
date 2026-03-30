namespace SectorGaza.WinForms;

public sealed class GameForm : Form
{
    private readonly GameController controller;
    private readonly GameRenderer renderer;
    private readonly System.Windows.Forms.Timer timer;

    public GameForm(GameController controller, GameRenderer renderer)
    {
        this.controller = controller;
        this.renderer = renderer;

        DoubleBuffered = true;
        KeyPreview = true;
        Text = "\u0421\u0435\u043A\u0442\u043E\u0440 \u0433\u0430\u0437\u0430";
        ClientSize = new Size(960, 640);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;

        timer = new System.Windows.Forms.Timer { Interval = 16 };
        timer.Tick += (_, _) =>
        {
            this.controller.Update();
            Invalidate();
        };

        KeyDown += (_, e) => this.controller.HandleKeyDown(e.KeyCode);
        KeyUp += (_, e) => this.controller.HandleKeyUp(e.KeyCode);
        Paint += (_, e) => this.renderer.Draw(e.Graphics, this.controller.GameWorld);
        Shown += (_, _) => timer.Start();
        FormClosed += (_, _) => timer.Stop();
    }
}
