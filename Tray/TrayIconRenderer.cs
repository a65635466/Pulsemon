using System.Drawing;
using System.Drawing.Drawing2D;

namespace PulseMon.Tray;

internal static class TrayIconRenderer
{
    private const int IconSize = 32;
    private const int PixelSize = 2;
    private const int GridSize = 16;
    private const int FrameCount = 8;

    public static Icon[] CreateRunningFrames()
    {
        var frames = new Icon[FrameCount];

        for (var index = 0; index < FrameCount; index++)
        {
            frames[index] = CreateFrame(index);
        }

        return frames;
    }

    private static Icon CreateFrame(int frameIndex)
    {
        using var bitmap = new Bitmap(IconSize, IconSize);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;

        var highMemory = frameIndex >= FrameCount / 2;
        var motionFrame = frameIndex % (FrameCount / 2);

        DrawPixelDog(graphics, motionFrame, highMemory);

        using var iconHandle = new SafeIconHandle(bitmap.GetHicon());
        return (Icon)Icon.FromHandle(iconHandle.DangerousGetHandle()).Clone();
    }

    private static void DrawPixelDog(Graphics graphics, int frameIndex, bool highMemory)
    {
        var coat = Color.FromArgb(255, 24, 24, 25);
        var coatHighlight = Color.FromArgb(255, 42, 42, 44);
        var tan = Color.FromArgb(255, 196, 154, 108);
        var eye = Color.FromArgb(255, 245, 248, 246);
        var nose = Color.FromArgb(255, 5, 5, 6);
        var cyan = Color.FromArgb(255, 0, 242, 255);

        using var coatBrush = new SolidBrush(coat);
        using var highlightBrush = new SolidBrush(coatHighlight);
        using var tanBrush = new SolidBrush(tan);
        using var eyeBrush = new SolidBrush(eye);
        using var noseBrush = new SolidBrush(nose);
        using var cyanBrush = new SolidBrush(cyan);

        // The ears, tan facial markings, and eye highlight stay fixed so the
        // small icon remains recognizable while only the legs and speed marks move.
        Pixel(graphics, coatBrush, 4, 1);
        Pixel(graphics, coatBrush, 3, 2);
        Pixel(graphics, coatBrush, 3, 3);
        Pixel(graphics, coatBrush, 4, 4);
        Pixel(graphics, coatBrush, 5, 3);
        Pixel(graphics, tanBrush, 4, 3);

        Pixel(graphics, coatBrush, 10, 1);
        Pixel(graphics, coatBrush, 11, 2);
        Pixel(graphics, coatBrush, 11, 3);
        Pixel(graphics, coatBrush, 10, 4);
        Pixel(graphics, coatBrush, 9, 3);
        Pixel(graphics, tanBrush, 10, 3);

        for (var y = 4; y <= 8; y++)
        {
            for (var x = 4; x <= 11; x++)
            {
                Pixel(graphics, coatBrush, x, y);
            }
        }

        Pixel(graphics, tanBrush, 5, 5);
        Pixel(graphics, tanBrush, 9, 5);
        Pixel(graphics, eyeBrush, 6, 6);
        Pixel(graphics, eyeBrush, 9, 6);
        Pixel(graphics, tanBrush, 6, 7);
        Pixel(graphics, tanBrush, 7, 7);
        Pixel(graphics, tanBrush, 8, 7);
        Pixel(graphics, tanBrush, 9, 7);
        Pixel(graphics, noseBrush, 7, 8);
        Pixel(graphics, noseBrush, 8, 8);

        for (var y = 9; y <= 11; y++)
        {
            for (var x = 5; x <= 10; x++)
            {
                Pixel(graphics, coatBrush, x, y);
            }
        }

        Pixel(graphics, highlightBrush, 5, 9);
        Pixel(graphics, highlightBrush, 10, 10);
        Pixel(graphics, coatBrush, 11, 9);
        Pixel(graphics, coatBrush, 12, 8);

        var legs = highMemory
            ? new[]
            {
                new Point(4, 12 + (frameIndex % 2)),
                new Point(6, 13 - (frameIndex % 2)),
                new Point(9, 13 - (frameIndex % 2)),
                new Point(11, 12 + (frameIndex % 2))
            }
            : new[]
            {
                new Point(5, 12 + (frameIndex % 2)),
                new Point(7, 13 - (frameIndex % 2)),
                new Point(9, 13 - (frameIndex % 2)),
                new Point(10, 12 + (frameIndex % 2))
            };

        foreach (var leg in legs)
        {
            Pixel(graphics, coatBrush, leg.X, leg.Y);
            Pixel(graphics, coatBrush, leg.X + (frameIndex % 2 == 0 ? 0 : (leg.X < 8 ? -1 : 1)), leg.Y + 1);
        }

        if (highMemory)
        {
            Pixel(graphics, cyanBrush, 1, 9 + (frameIndex % 2));
            Pixel(graphics, cyanBrush, 2, 9 + (frameIndex % 2));
            Pixel(graphics, cyanBrush, 2, 12 - (frameIndex % 2));
            Pixel(graphics, cyanBrush, 3, 12 - (frameIndex % 2));
        }
    }

    private static void Pixel(Graphics graphics, Brush brush, int x, int y)
    {
        if (x < 0 || x >= GridSize || y < 0 || y >= GridSize)
        {
            return;
        }

        graphics.FillRectangle(brush, x * PixelSize, y * PixelSize, PixelSize, PixelSize);
    }
}
