namespace SectorGaza.WinForms;

public sealed class GameForm : Form
{
    private const int ButtonSpacing = 14;
    private const int TitleGap = 30;

    private enum ScreenState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    private readonly GameController controller;
    private readonly GameRenderer renderer;
    private readonly System.Windows.Forms.Timer timer;
    private readonly Button startButton;
    private readonly Button continueButton;
    private readonly Button restartButton;
    private readonly Button exitButton;
    private readonly Label titleLabel;

    private ScreenState state = ScreenState.Menu;

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

        titleLabel = new Label
        {
            AutoSize = false,
            Size = new Size(380, 70),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 30F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(232, 239, 247),
            BackColor = Color.Transparent,
            Text = "\u0421\u0435\u043A\u0442\u043E\u0440 \u0433\u0430\u0437\u0430"
        };

        startButton = BuildButton(
            "\u041D\u0430\u0447\u0430\u0442\u044C",
            new Point(0, 0),
            StartGame);

        continueButton = BuildButton(
            "\u041F\u0440\u043E\u0434\u043E\u043B\u0436\u0438\u0442\u044C",
            new Point(0, 0),
            ResumeGame);

        restartButton = BuildButton(
            "\u041D\u0430\u0447\u0430\u0442\u044C \u0441\u043D\u0430\u0447\u0430\u043B\u0430",
            new Point(0, 0),
            RestartGame);

        exitButton = BuildButton(
            "\u0412\u044B\u0439\u0442\u0438",
            new Point(0, 0),
            ExitGame);

        Controls.Add(titleLabel);
        Controls.Add(startButton);
        Controls.Add(continueButton);
        Controls.Add(restartButton);
        Controls.Add(exitButton);
        ConfigureScreenState();

        timer = new System.Windows.Forms.Timer { Interval = 16 };
        timer.Tick += (_, _) =>
        {
            if (state == ScreenState.Playing)
            {
                this.controller.Update();
                if (this.controller.GameWorld.IsGameOver)
                {
                    state = ScreenState.GameOver;
                    controller.ResetInput();
                    ConfigureScreenState();
                }
            }

            Invalidate();
        };

        KeyDown += HandleKeyDown;
        KeyUp += HandleKeyUp;
        Paint += (_, e) => this.renderer.Draw(
            e.Graphics,
            this.controller.GameWorld,
            showHud: state == ScreenState.Playing);
        Paint += (_, e) =>
        {
            if (state != ScreenState.Playing)
            {
                this.renderer.DrawMenuOverlay(e.Graphics, ClientSize);
            }
        };

        Shown += (_, _) =>
        {
            CenterMenuControls();
            Invalidate();
            timer.Start();
        };
        FormClosed += (_, _) => timer.Stop();
        Resize += (_, _) => CenterMenuControls();
        Layout += (_, _) =>
        {
            if (state != ScreenState.Playing)
            {
                CenterMenuControls();
            }
        };
    }

    private Button BuildButton(string text, Point location, Action onClick)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(230, 52),
            Location = location,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point),
            BackColor = Color.FromArgb(66, 12, 21, 27),
            UseVisualStyleBackColor = false,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(240, 244, 248),
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(170, 170, 205, 220);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 230, 238, 244);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(65, 170, 195, 208);
        button.Click += (_, _) => onClick();
        return button;
    }

    private void StartGame()
    {
        controller.StartNewGame();
        state = ScreenState.Playing;
        ConfigureScreenState();
        Focus();
    }

    private void RestartGame()
    {
        controller.StartNewGame();
        state = ScreenState.Playing;
        ConfigureScreenState();
        Focus();
    }

    private void ResumeGame()
    {
        state = ScreenState.Playing;
        controller.ResetInput();
        ConfigureScreenState();
        Focus();
    }

    private void ExitGame()
    {
        Close();
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (state == ScreenState.Playing)
        {
            if (e.KeyCode == Keys.Escape)
            {
                state = ScreenState.Paused;
                controller.ResetInput();
                ConfigureScreenState();
                return;
            }

            controller.HandleKeyDown(e.KeyCode);
            return;
        }

        if (state == ScreenState.Paused && e.KeyCode == Keys.Escape)
        {
            ResumeGame();
            return;
        }
    }

    private void HandleKeyUp(object? sender, KeyEventArgs e)
    {
        if (state != ScreenState.Playing)
        {
            return;
        }

        controller.HandleKeyUp(e.KeyCode);
    }

    private void ConfigureScreenState()
    {
        titleLabel.Visible = state != ScreenState.Playing;
        startButton.Visible = state == ScreenState.Menu;
        continueButton.Visible = state == ScreenState.Paused;
        restartButton.Visible = state == ScreenState.Paused || state == ScreenState.GameOver;
        exitButton.Visible = state == ScreenState.Menu || state == ScreenState.Paused || state == ScreenState.GameOver;

        titleLabel.Text = state switch
        {
            ScreenState.Menu => "\u0421\u0435\u043A\u0442\u043E\u0440 \u0433\u0430\u0437\u0430",
            ScreenState.Paused => "\u041F\u0430\u0443\u0437\u0430",
            ScreenState.GameOver => "\u0412\u044B \u043F\u043E\u0433\u0438\u0431\u043B\u0438",
            _ => "\u0421\u0435\u043A\u0442\u043E\u0440 \u0433\u0430\u0437\u0430"
        };

        CenterMenuControls();
    }

    private void CenterMenuControls()
    {
        if (state == ScreenState.Playing)
        {
            return;
        }

        var visibleButtons = new List<Button>();
        if (startButton.Visible)
        {
            visibleButtons.Add(startButton);
        }

        if (continueButton.Visible)
        {
            visibleButtons.Add(continueButton);
        }

        if (restartButton.Visible)
        {
            visibleButtons.Add(restartButton);
        }

        if (exitButton.Visible)
        {
            visibleButtons.Add(exitButton);
        }

        var centerX = ClientSize.Width / 2;
        var buttonsHeight = visibleButtons.Count == 0
            ? 0
            : (visibleButtons.Count * startButton.Height) + ((visibleButtons.Count - 1) * ButtonSpacing);
        var menuGroupHeight = titleLabel.Height + TitleGap + buttonsHeight;
        var menuGroupTop = Math.Max(24, (ClientSize.Height - menuGroupHeight) / 2);

        titleLabel.Location = new Point(centerX - (titleLabel.Width / 2), menuGroupTop);
        var firstY = titleLabel.Bottom + TitleGap;

        for (var index = 0; index < visibleButtons.Count; index++)
        {
            var button = visibleButtons[index];
            var y = firstY + (index * (button.Height + ButtonSpacing));
            button.Location = new Point(centerX - (button.Width / 2), y);
        }
    }
}
