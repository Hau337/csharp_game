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
        DrawWallShadows(graphics, room.Walls);
        DrawWalls(graphics, room.Walls);
        DrawEnemyShadow(graphics, gameWorld.Enemy);
        DrawEnemy(graphics, gameWorld.Enemy);
        DrawPlayerShadow(graphics, gameWorld.Player);
        DrawPlayer(graphics, gameWorld.Player);
        if (showHud)
        {
            DrawHud(graphics, gameWorld);
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
        var panelBounds = new Rectangle(18, 460, 290, 158);
        using var panelBrush = new SolidBrush(Color.FromArgb(120, 6, 10, 14));
        using var panelBorder = new Pen(Color.FromArgb(120, 160, 190, 205), 1);
        using var titleFont = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
        using var textFont = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(HudColor);
        using var hpBackBrush = new SolidBrush(Color.FromArgb(115, 34, 45, 52));
        using var hpFillBrush = new SolidBrush(GetHpColor(gameWorld.Player.CurrentHealth, Player.MaxHealth));

        graphics.FillRectangle(panelBrush, panelBounds);
        graphics.DrawRectangle(panelBorder, panelBounds);

        graphics.DrawString("\u0421\u0442\u0430\u0442\u0443\u0441", titleFont, brush, 30, 476);

        var hpBarRect = new Rectangle(30, 502, 250, 16);
        graphics.FillRectangle(hpBackBrush, hpBarRect);
        var hpRatio = Math.Clamp(gameWorld.Player.CurrentHealth / (double)Player.MaxHealth, 0.0, 1.0);
        var hpFillWidth = (int)Math.Round(hpBarRect.Width * hpRatio);
        if (hpFillWidth > 0)
        {
            graphics.FillRectangle(hpFillBrush, new Rectangle(hpBarRect.X, hpBarRect.Y, hpFillWidth, hpBarRect.Height));
        }
        graphics.DrawRectangle(Pens.Black, hpBarRect);
        graphics.DrawString($"HP: {gameWorld.Player.CurrentHealth}/{Player.MaxHealth}", textFont, brush, 30, 522);

        graphics.DrawString($"\u0412\u0440\u0430\u0433\u043E\u0432 \u043E\u0441\u0442\u0430\u043B\u043E\u0441\u044C: {gameWorld.AliveEnemies}/{gameWorld.TotalEnemies}", textFont, brush, 30, 546);
        graphics.DrawString("WASD - \u0434\u0432\u0438\u0436\u0435\u043D\u0438\u0435", textFont, brush, 30, 570);
        graphics.DrawString("\u041F\u0440\u043E\u0431\u0435\u043B - \u0430\u0442\u0430\u043A\u0430", textFont, brush, 30, 592);
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
