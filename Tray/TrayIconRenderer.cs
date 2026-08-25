using System.Drawing;
using System.Drawing.Drawing2D;

namespace PulseMon.Tray;

internal static class TrayIconRenderer
{
    private const int IconSize = 32;
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
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        DrawDog(graphics, frameIndex % 4, frameIndex >= FrameCount / 2);

        using var iconHandle = new SafeIconHandle(bitmap.GetHicon());
        return (Icon)Icon.FromHandle(iconHandle.DangerousGetHandle()).Clone();
    }

    private static void DrawDog(Graphics graphics, int motionFrame, bool highMemory)
    {
        using var coat = new SolidBrush(Color.FromArgb(255, 20, 20, 22));
        using var coatHighlight = new SolidBrush(Color.FromArgb(255, 48, 48, 52));
        using var eyebrow = new SolidBrush(Color.FromArgb(255, 145, 91, 48));
        using var muzzle = new SolidBrush(Color.FromArgb(255, 126, 83, 53));
        using var eye = new SolidBrush(Color.FromArgb(255, 248, 248, 238));
        using var nose = new SolidBrush(Color.FromArgb(255, 4, 4, 5));
        using var blue = new SolidBrush(Color.FromArgb(255, 35, 105, 190));
        using var blueLight = new SolidBrush(Color.FromArgb(255, 74, 145, 225));
        using var red = new SolidBrush(Color.FromArgb(255, 205, 42, 38));
        using var white = new SolidBrush(Color.FromArgb(255, 242, 246, 252));
        using var speed = new SolidBrush(Color.FromArgb(255, 238, 180, 42));

        var bounce = motionFrame is 1 or 3 ? -1 : 0;
        var body = new RectangleF(9, 15 + bounce, 17, 8);
        var head = new RectangleF(3, 6 + bounce, 11, 12);

        // Upright Chihuahua ears remain readable at tray-icon sizes.
        graphics.FillPolygon(coat, new[]
        {
            new PointF(4, 8 + bounce), new PointF(4, 1 + bounce), new PointF(8, 7 + bounce)
        });
        graphics.FillPolygon(coat, new[]
        {
            new PointF(10, 7 + bounce), new PointF(12, 1 + bounce), new PointF(15, 9 + bounce)
        });

        graphics.FillEllipse(coat, head);
        graphics.FillEllipse(muzzle, new RectangleF(2, 13 + bounce, 7, 5));
        graphics.FillEllipse(nose, new RectangleF(1, 14 + bounce, 3, 3));
        graphics.FillEllipse(eye, new RectangleF(8, 10 + bounce, 2.2f, 2.2f));
        graphics.FillEllipse(nose, new RectangleF(8.7f, 10.6f + bounce, 0.9f, 0.9f));
        graphics.FillRectangle(eyebrow, new RectangleF(7.5f, 8.7f + bounce, 3.2f, 1.2f));

        graphics.FillRectangle(red, new RectangleF(7, 17 + bounce, 8, 2));
        graphics.FillRoundedRectangle(blue, body, 2);
        graphics.FillRectangle(blueLight, new RectangleF(10, 16 + bounce, 2, 6));

        // A compact white PulseMon mark stays legible in the larger shortcut icon.
        graphics.FillRectangle(white, new RectangleF(14, 17 + bounce, 1, 4));
        graphics.FillRectangle(white, new RectangleF(15, 17 + bounce, 2, 1));
        graphics.FillRectangle(white, new RectangleF(15, 19 + bounce, 1, 1));

        var legOffset = motionFrame switch
        {
            0 => new[] { -1, 1, 1, -1 },
            1 => new[] { 0, -1, 2, 0 },
            2 => new[] { 1, -1, -1, 1 },
            _ => new[] { 0, 1, -1, 0 }
        };
        var legX = new[] { 11f, 15f, 20f, 23f };
        for (var index = 0; index < legX.Length; index++)
        {
            var x = legX[index];
            var y = 22 + legOffset[index] + bounce;
            graphics.FillRoundedRectangle(coat, new RectangleF(x, y, 2.2f, 6), 1);
        }

        graphics.FillEllipse(coat, new RectangleF(24, 12 + bounce, 7, 8));
        graphics.DrawArc(new Pen(coat, 2.2f), new RectangleF(24, 4 + bounce, 7, 12), 285, 205);

        if (highMemory)
        {
            using var speedPen = new Pen(speed, 1.5f);
            graphics.DrawLine(speedPen, 1, 23 + bounce, 4, 23 + bounce);
            graphics.DrawLine(speedPen, 2, 26 + bounce, 5, 26 + bounce);
        }
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF rectangle, float radius)
    {
        using var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
