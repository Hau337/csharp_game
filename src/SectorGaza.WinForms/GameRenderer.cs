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
    private readonly Image? labTileset = LoadImage("assets", "tiles", "lab_tileset.png");

    public void Draw(Graphics graphics, GameWorld gameWorld)
    {
        graphics.Clear(BackgroundColor);
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        DrawFloor(graphics, gameWorld.Room);
        DrawWallShadows(graphics, gameWorld.Room.Walls);
        DrawWalls(graphics, gameWorld.Room.Walls);
        DrawPlayerShadow(graphics, gameWorld.Player);
        DrawPlayer(graphics, gameWorld.Player);
        DrawHud(graphics);
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
    }

    private static void DrawHud(Graphics graphics)
    {
        var panelBounds = new Rectangle(22, 20, 160, 38);
        using var panelBrush = new SolidBrush(Color.FromArgb(120, 6, 10, 14));
        using var panelBorder = new Pen(Color.FromArgb(120, 160, 190, 205), 1);
        using var font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
        using var brush = new SolidBrush(HudColor);

        graphics.FillRectangle(panelBrush, panelBounds);
        graphics.DrawRectangle(panelBorder, panelBounds);
        graphics.DrawString("\u0421\u0435\u043A\u0442\u043E\u0440 \u0433\u0430\u0437\u0430", font, brush, 34, 29);
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
