using SectorGaza.Model;
using System.Drawing.Drawing2D;

namespace SectorGaza.WinForms;

public sealed class GameRenderer
{
    private const int TileSize = 32;
    private static readonly Color BackgroundColor = Color.FromArgb(10, 14, 18);
    private static readonly Color FloorFallbackColor = Color.FromArgb(18, 63, 69);
    private static readonly Color WallFallbackColor = Color.FromArgb(81, 88, 112);
    private static readonly Color ShadowColor = Color.FromArgb(90, 0, 0, 0);
    private static readonly Color HudColor = Color.FromArgb(235, 239, 242);
    private static readonly Color LabGridColor = Color.FromArgb(24, 164, 188, 202);
    private static readonly Color LabLightColor = Color.FromArgb(72, 142, 220, 236);
    private static readonly Rectangle FloorTileSource = new(0, 160, 16, 16);
    private static readonly Rectangle WallTileSource = new(0, 0, 16, 16);
    private static readonly Rectangle AccentTileSource = new(80, 0, 16, 16);

    private readonly Image? playerSprite = LoadImage("assets", "sprites", "player.png");
    private readonly Image? enemySprite = LoadImage("assets", "sprites", "mutant.png");
    private readonly Image? labTileset = LoadImage("assets", "tiles", "lab_tileset.png");

    public void Draw(Graphics graphics, GameWorld gameWorld, bool showHud)
    {
        graphics.Clear(BackgroundColor);
        var state = graphics.Save();
        var clip = graphics.VisibleClipBounds;
        var room = gameWorld.Room;
        var scaleX = clip.Width / room.Width;
        var scaleY = clip.Height / room.Height;
        graphics.ScaleTransform(scaleX, scaleY);
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        DrawFloor(graphics, room);
        DrawLabDecor(graphics, room);
        DrawWallShadows(graphics, room.Walls);
        DrawWalls(graphics, room.Walls);
        DrawTransitions(graphics, gameWorld.Transitions);
        DrawMedkits(graphics, gameWorld.Medkits);
        DrawNotes(graphics, gameWorld.Notes);
        DrawKeyCard(graphics, gameWorld.KeyCard);
        DrawFinalDoor(graphics, gameWorld.FinalDoor, gameWorld.HasKeyCard, gameWorld.IsCurrentRoomCleared);
        foreach (var enemy in gameWorld.Enemies)
        {
            DrawEnemyShadow(graphics, enemy);
        }

        foreach (var enemy in gameWorld.Enemies)
        {
            DrawEnemy(graphics, enemy);
        }

        DrawPlayerShadow(graphics, gameWorld.Player);
        DrawPlayer(graphics, gameWorld.Player);
        if (showHud)
        {
            DrawHud(graphics, gameWorld);
            DrawStoryMessage(graphics, gameWorld.ActiveStoryMessage, room.Width);
        }

        graphics.Restore(state);
    }

    public void DrawMenuOverlay(Graphics graphics, Size clientSize)
    {
        var area = new Rectangle(Point.Empty, clientSize);
        if (area.Width <= 0 || area.Height <= 0)
        {
            return;
        }

        using var fogBrush = new SolidBrush(Color.FromArgb(120, 8, 11, 14));
        using var gradientBrush = new LinearGradientBrush(
            area,
            Color.FromArgb(92, 32, 45, 58),
            Color.FromArgb(128, 7, 12, 18),
            90f);
        using var scanlinePen = new Pen(Color.FromArgb(10, 220, 232, 242), 1);

        graphics.FillRectangle(fogBrush, area);
        graphics.FillRectangle(gradientBrush, area);

        DrawSoftSpot(
            graphics,
            new Point((int)(area.Width * 0.24f), (int)(area.Height * 0.35f)),
            Math.Max(180, area.Width / 5),
            Color.FromArgb(48, 155, 201, 222));

        DrawSoftSpot(
            graphics,
            new Point((int)(area.Width * 0.76f), (int)(area.Height * 0.64f)),
            Math.Max(220, area.Width / 4),
            Color.FromArgb(42, 104, 176, 193));

        for (var y = 0; y < area.Height; y += 3)
        {
            graphics.DrawLine(scanlinePen, 0, y, area.Width, y);
        }
    }

    private void DrawFloor(Graphics graphics, Room room)
    {
        using var fallbackBrush = new SolidBrush(FloorFallbackColor);
        graphics.FillRectangle(fallbackBrush, 0, 0, room.Width, room.Height);

        if (labTileset is null)
        {
            return;
        }

        for (var y = 0; y < room.Height; y += TileSize)
        {
            for (var x = 0; x < room.Width; x += TileSize)
            {
                var destination = new Rectangle(x, y, TileSize, TileSize);
                graphics.DrawImage(labTileset, destination, FloorTileSource, GraphicsUnit.Pixel);
            }
        }
    }

    private static void DrawLabDecor(Graphics graphics, Room room)
    {
        using var gridPen = new Pen(LabGridColor, 1);
        using var lightBrush = new SolidBrush(LabLightColor);
        using var dimLightBrush = new SolidBrush(Color.FromArgb(45, 114, 176, 194));

        for (var x = 0; x < room.Width; x += 96)
        {
            graphics.DrawLine(gridPen, x, 0, x, room.Height);
        }

        for (var y = 0; y < room.Height; y += 96)
        {
            graphics.DrawLine(gridPen, 0, y, room.Width, y);
        }

        var lampWidth = 68;
        var lampHeight = 8;
        for (var x = 140; x < room.Width - 140; x += 220)
        {
            graphics.FillRectangle(lightBrush, new Rectangle(x, 18, lampWidth, lampHeight));
            graphics.FillRectangle(dimLightBrush, new Rectangle(x + 36, room.Height - 26, lampWidth - 14, lampHeight));
        }
    }

    private static void DrawWallShadows(Graphics graphics, IReadOnlyList<Wall> walls)
    {
        using var shadowBrush = new SolidBrush(ShadowColor);
        foreach (var wall in walls)
        {
            var bounds = wall.Bounds;
            graphics.FillRectangle(shadowBrush, bounds.X + 6, bounds.Y + 6, bounds.Width, bounds.Height);
        }
    }

    private void DrawWalls(Graphics graphics, IReadOnlyList<Wall> walls)
    {
        using var fallbackBrush = new SolidBrush(WallFallbackColor);

        foreach (var wall in walls)
        {
            var bounds = wall.Bounds;
            graphics.FillRectangle(fallbackBrush, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            DrawWallTiles(graphics, bounds);
            DrawWallBorder(graphics, bounds);
        }
    }

    private void DrawWallTiles(Graphics graphics, IntRectangle bounds)
    {
        if (labTileset is null)
        {
            return;
        }

        var tileIndex = 0;
        for (var y = bounds.Y; y < bounds.Bottom; y += TileSize)
        {
            for (var x = bounds.X; x < bounds.Right; x += TileSize)
            {
                var tileWidth = Math.Min(TileSize, bounds.Right - x);
                var tileHeight = Math.Min(TileSize, bounds.Bottom - y);
                var destination = new Rectangle(x, y, tileWidth, tileHeight);
                var source = tileIndex % 3 == 1 ? AccentTileSource : WallTileSource;
                graphics.DrawImage(labTileset, destination, source, GraphicsUnit.Pixel);
                tileIndex++;
            }
        }
    }

    private static void DrawWallBorder(Graphics graphics, IntRectangle bounds)
    {
        using var brightPen = new Pen(Color.FromArgb(160, 198, 214, 228), 2);
        using var darkPen = new Pen(Color.FromArgb(120, 22, 29, 36), 2);

        graphics.DrawLine(brightPen, bounds.X, bounds.Y, bounds.Right, bounds.Y);
        graphics.DrawLine(brightPen, bounds.X, bounds.Y, bounds.X, bounds.Bottom);
        graphics.DrawLine(darkPen, bounds.X, bounds.Bottom, bounds.Right, bounds.Bottom);
        graphics.DrawLine(darkPen, bounds.Right, bounds.Y, bounds.Right, bounds.Bottom);
    }

    private static void DrawPlayerShadow(Graphics graphics, Player player)
    {
        var bounds = player.Bounds;
        using var brush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
        graphics.FillEllipse(brush, bounds.X + 5, bounds.Y + bounds.Height - 4, bounds.Width - 6, 10);
    }

    private static void DrawEnemyShadow(Graphics graphics, Enemy enemy)
    {
        if (!enemy.IsAlive)
        {
            return;
        }

        var bounds = enemy.Bounds;
        using var brush = new SolidBrush(Color.FromArgb(75, 0, 0, 0));
        graphics.FillEllipse(brush, bounds.X + 4, bounds.Y + bounds.Height - 4, bounds.Width - 6, 9);
    }

    private void DrawEnemy(Graphics graphics, Enemy enemy)
    {
        if (!enemy.IsAlive)
        {
            return;
        }

        var bounds = enemy.Bounds;
        if (enemySprite is null)
        {
            using var fallbackBrush = new SolidBrush(Color.FromArgb(214, 83, 83));
            graphics.FillEllipse(fallbackBrush, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            return;
        }

        var state = graphics.Save();
        var centerX = bounds.X + (bounds.Width / 2f);
        var centerY = bounds.Y + (bounds.Height / 2f);

        graphics.TranslateTransform(centerX, centerY);
        graphics.RotateTransform((float)enemy.FacingAngleDegrees);
        graphics.DrawImage(enemySprite, new Rectangle(-18, -18, 36, 36), 0, 0, enemySprite.Width, enemySprite.Height, GraphicsUnit.Pixel);

        if (enemy.Kind == EnemyKind.Fast)
        {
            using var fastEnemyAura = new Pen(Color.FromArgb(170, 112, 208, 238), 2);
            graphics.DrawEllipse(fastEnemyAura, -21, -21, 42, 42);
        }

        graphics.Restore(state);
    }

    private void DrawPlayer(Graphics graphics, Player player)
    {
        var bounds = player.Bounds;
        if (playerSprite is null)
        {
            using var fallbackBrush = new SolidBrush(Color.FromArgb(145, 217, 100));
            graphics.FillEllipse(fallbackBrush, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            return;
        }

        var state = graphics.Save();
        var centerX = bounds.X + (bounds.Width / 2f);
        var centerY = bounds.Y + (bounds.Height / 2f);

        graphics.TranslateTransform(centerX, centerY);
        graphics.RotateTransform((float)player.FacingAngleDegrees);
        graphics.DrawImage(playerSprite, new Rectangle(-18, -18, 36, 36), 0, 0, playerSprite.Width, playerSprite.Height, GraphicsUnit.Pixel);
        graphics.Restore(state);

        if (player.IsAttackFlashVisible)
        {
            DrawAttackFlash(graphics, player);
        }
    }

    private static void DrawAttackFlash(Graphics graphics, Player player)
    {
        var state = graphics.Save();
        var bounds = player.Bounds;
        var centerX = bounds.X + (bounds.Width / 2f);
        var centerY = bounds.Y + (bounds.Height / 2f);
        var flashRect = new Rectangle(-42, -42, 84, 84);

        graphics.TranslateTransform(centerX, centerY);
        graphics.RotateTransform((float)player.FacingAngleDegrees);
        using var pen = new Pen(Color.FromArgb(220, 255, 210, 120), 3);
        graphics.DrawArc(pen, flashRect, -35, 70);
        graphics.Restore(state);
    }

    private static void DrawHud(Graphics graphics, GameWorld gameWorld)
    {
        const int margin = 16;
        const int panelWidth = 280;
        const int panelHeight = 224;
        var panelBounds = new Rectangle(margin, gameWorld.Room.Height - panelHeight - margin, panelWidth, panelHeight);
        using var panelBrush = new SolidBrush(Color.FromArgb(120, 6, 10, 14));
        using var panelBorder = new Pen(Color.FromArgb(120, 160, 190, 205), 1);
        using var titleFont = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
        using var textFont = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(HudColor);
        using var hintBrush = new SolidBrush(Color.FromArgb(232, 168, 215, 228));
        using var hpBackBrush = new SolidBrush(Color.FromArgb(115, 34, 45, 52));
        using var hpFillBrush = new SolidBrush(GetHpColor(gameWorld.Player.CurrentHealth, Player.MaxHealth));

        graphics.FillRectangle(panelBrush, panelBounds);
        graphics.DrawRectangle(panelBorder, panelBounds);

        var left = panelBounds.X + 12;
        var y = panelBounds.Y + 8;
        graphics.DrawString("\u0421\u0442\u0430\u0442\u0443\u0441", titleFont, brush, left, y);
        y += 18;

        graphics.DrawString($"\u041A\u043E\u043C\u043D\u0430\u0442\u0430: {gameWorld.CurrentRoomNumber}/{gameWorld.TotalRooms}", textFont, brush, left, y);
        y += 15;
        graphics.DrawString(gameWorld.CurrentRoomName, textFont, brush, left, y);
        y += 16;

        var hpBarRect = new Rectangle(left, y, panelBounds.Width - 24, 12);
        graphics.FillRectangle(hpBackBrush, hpBarRect);
        var hpRatio = Math.Clamp(gameWorld.Player.CurrentHealth / (double)Player.MaxHealth, 0.0, 1.0);
        var hpFillWidth = (int)Math.Round(hpBarRect.Width * hpRatio);
        if (hpFillWidth > 0)
        {
            graphics.FillRectangle(hpFillBrush, new Rectangle(hpBarRect.X, hpBarRect.Y, hpFillWidth, hpBarRect.Height));
        }
        graphics.DrawRectangle(Pens.Black, hpBarRect);
        y += 14;
        graphics.DrawString($"HP: {gameWorld.Player.CurrentHealth}/{Player.MaxHealth}", textFont, brush, left, y);
        y += 15;
        graphics.DrawString($"\u0412\u0440\u0430\u0433\u043E\u0432: {gameWorld.AliveEnemies}/{gameWorld.TotalEnemies}", textFont, brush, left, y);
        y += 14;
        graphics.DrawString($"\u0417\u0430\u043F\u0438\u0441\u043A\u0438: {gameWorld.CollectedNotesCount}/{gameWorld.TotalNotes}", textFont, brush, left, y);
        y += 14;
        graphics.DrawString($"\u041A\u043B\u044E\u0447-\u043A\u0430\u0440\u0442\u0430: {(gameWorld.HasKeyCard ? "\u0435\u0441\u0442\u044C" : "\u043D\u0435\u0442")}", textFont, brush, left, y);

        if (!gameWorld.IsCurrentRoomCleared && gameWorld.CurrentRoomNumber < gameWorld.TotalRooms)
        {
            y += 14;
            graphics.DrawString("\u0414\u0432\u0435\u0440\u044C \u043E\u0442\u043A\u0440\u043E\u0435\u0442\u0441\u044F \u043F\u043E\u0441\u043B\u0435 \u0437\u0430\u0447\u0438\u0441\u0442\u043A\u0438 \u0432\u0440\u0430\u0433\u043E\u0432", textFont, brush, left, y);
        }

        y += 15;
        graphics.DrawString("WASD - \u0434\u0432\u0438\u0436\u0435\u043D\u0438\u0435", textFont, brush, left, y);
        y += 13;
        graphics.DrawString("\u041F\u0440\u043E\u0431\u0435\u043B - \u0430\u0442\u0430\u043A\u0430", textFont, brush, left, y);
        y += 13;
        graphics.DrawString("E - \u0432\u0437\u0430\u0438\u043C\u043E\u0434\u0435\u0439\u0441\u0442\u0432\u0438\u0435", textFont, brush, left, y);
        y += 13;
        graphics.DrawString("Esc - \u043F\u0430\u0443\u0437\u0430", textFont, brush, left, y);

        var hint = gameWorld.InteractionHint;
        if (!string.IsNullOrWhiteSpace(hint))
        {
            y += 16;
            graphics.DrawString(hint, textFont, hintBrush, left, y);
        }
    }

    private static void DrawNotes(Graphics graphics, IReadOnlyList<StoryNote> notes)
    {
        using var noteBrush = new SolidBrush(Color.FromArgb(232, 214, 202, 170));
        using var noteBorder = new Pen(Color.FromArgb(180, 74, 64, 48), 1);

        foreach (var note in notes)
        {
            if (note.IsCollected)
            {
                continue;
            }

            var bounds = note.Bounds;
            var rect = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            graphics.FillRectangle(noteBrush, rect);
            graphics.DrawRectangle(noteBorder, rect);
            graphics.DrawLine(noteBorder, rect.X + 4, rect.Y + 6, rect.Right - 4, rect.Y + 6);
            graphics.DrawLine(noteBorder, rect.X + 4, rect.Y + 11, rect.Right - 4, rect.Y + 11);
        }
    }

    private static void DrawKeyCard(Graphics graphics, KeyCard? keyCard)
    {
        if (keyCard is null || keyCard.IsCollected)
        {
            return;
        }

        var bounds = keyCard.Bounds;
        var rect = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        using var bodyBrush = new SolidBrush(Color.FromArgb(225, 229, 190, 84));
        using var borderPen = new Pen(Color.FromArgb(200, 70, 58, 36), 1);
        using var chipBrush = new SolidBrush(Color.FromArgb(215, 44, 50, 60));

        graphics.FillRectangle(bodyBrush, rect);
        graphics.DrawRectangle(borderPen, rect);
        graphics.FillRectangle(chipBrush, rect.X + 4, rect.Y + 4, 8, rect.Height - 8);
    }

    private static void DrawFinalDoor(Graphics graphics, FinalDoor? finalDoor, bool hasKeyCard, bool isRoomCleared)
    {
        if (finalDoor is null)
        {
            return;
        }

        var bounds = finalDoor.Bounds;
        var rect = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        var color = finalDoor.IsOpened
            ? Color.FromArgb(210, 92, 196, 130)
            : hasKeyCard && isRoomCleared
                ? Color.FromArgb(210, 188, 204, 92)
                : Color.FromArgb(210, 167, 82, 82);

        using var doorBrush = new SolidBrush(color);
        using var borderPen = new Pen(Color.FromArgb(190, 24, 34, 42), 2);
        graphics.FillRectangle(doorBrush, rect);
        graphics.DrawRectangle(borderPen, rect);
    }

    private static void DrawStoryMessage(Graphics graphics, string? message, int roomWidth)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var width = Math.Min(760, roomWidth - 40);
        var bounds = new Rectangle((roomWidth - width) / 2, 18, width, 56);
        using var panelBrush = new SolidBrush(Color.FromArgb(160, 8, 12, 16));
        using var panelBorder = new Pen(Color.FromArgb(170, 160, 189, 206), 1);
        using var textFont = new Font("Segoe UI", 8.8F, FontStyle.Regular, GraphicsUnit.Point);
        using var textBrush = new SolidBrush(Color.FromArgb(232, 237, 242));
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisWord,
            FormatFlags = StringFormatFlags.LineLimit
        };

        graphics.FillRectangle(panelBrush, bounds);
        graphics.DrawRectangle(panelBorder, bounds);
        graphics.DrawString(message, textFont, textBrush, bounds, format);
    }

    private static void DrawTransitions(Graphics graphics, IReadOnlyList<RoomTransition> transitions)
    {
        using var fillBrush = new SolidBrush(Color.FromArgb(30, 94, 184, 214));
        using var borderPen = new Pen(Color.FromArgb(170, 130, 220, 238), 2);

        foreach (var transition in transitions)
        {
            var bounds = transition.TriggerBounds;
            var drawRect = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            graphics.FillRectangle(fillBrush, drawRect);
            graphics.DrawRectangle(borderPen, drawRect);
        }
    }

    private static void DrawMedkits(Graphics graphics, IReadOnlyList<Medkit> medkits)
    {
        using var bodyBrush = new SolidBrush(Color.FromArgb(218, 176, 48, 48));
        using var crossBrush = new SolidBrush(Color.FromArgb(226, 238, 246, 250));
        using var borderPen = new Pen(Color.FromArgb(160, 20, 28, 34), 1);

        foreach (var medkit in medkits)
        {
            if (medkit.IsCollected)
            {
                continue;
            }

            var bounds = medkit.Bounds;
            var rect = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            graphics.FillRectangle(bodyBrush, rect);
            graphics.DrawRectangle(borderPen, rect);

            var centerX = rect.X + (rect.Width / 2);
            var centerY = rect.Y + (rect.Height / 2);
            graphics.FillRectangle(crossBrush, centerX - 2, rect.Y + 4, 4, rect.Height - 8);
            graphics.FillRectangle(crossBrush, rect.X + 4, centerY - 2, rect.Width - 8, 4);
        }
    }

    private static void DrawSoftSpot(Graphics graphics, Point center, int radius, Color color)
    {
        var diameter = radius * 2;
        var bounds = new Rectangle(center.X - radius, center.Y - radius, diameter, diameter);

        using var path = new GraphicsPath();
        path.AddEllipse(bounds);

        using var brush = new PathGradientBrush(path)
        {
            CenterColor = color,
            SurroundColors = new[] { Color.FromArgb(0, color.R, color.G, color.B) }
        };

        graphics.FillEllipse(brush, bounds);
    }

    private static Color GetHpColor(int hp, int maxHp)
    {
        var ratio = maxHp <= 0 ? 0 : hp / (double)maxHp;
        if (ratio > 0.55)
        {
            return Color.FromArgb(220, 90, 196, 114);
        }

        if (ratio > 0.25)
        {
            return Color.FromArgb(220, 219, 176, 82);
        }

        return Color.FromArgb(220, 210, 90, 90);
    }

    private static Image? LoadImage(params string[] relativeParts)
    {
        var path = Path.Combine(new[] { AppContext.BaseDirectory }.Concat(relativeParts).ToArray());
        if (!File.Exists(path))
        {
            return null;
        }

        return Image.FromFile(path);
    }
}
