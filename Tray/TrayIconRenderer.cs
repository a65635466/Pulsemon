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

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var backgroundBrush = new SolidBrush(Color.FromArgb(255, 21, 24, 29));
        graphics.FillEllipse(backgroundBrush, 2, 2, 28, 28);

        using var basePen = new Pen(Color.FromArgb(255, 45, 52, 61), 3);
        graphics.DrawEllipse(basePen, 5, 5, 22, 22);

        var startAngle = frameIndex * 45 - 90;
        using var cyanPen = new Pen(Color.FromArgb(255, 54, 214, 231), 3)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        using var greenPen = new Pen(Color.FromArgb(255, 69, 224, 143), 3)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        graphics.DrawArc(cyanPen, 5, 5, 22, 22, startAngle, 82);
        graphics.DrawArc(greenPen, 5, 5, 22, 22, startAngle + 180, 54);

        using var pulsePen = new Pen(Color.FromArgb(255, 242, 245, 248), 2)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        var pulsePoints = new[]
        {
            new PointF(8, 16),
            new PointF(12, 16),
            new PointF(14, 12),
            new PointF(17, 21),
            new PointF(20, 16),
            new PointF(25, 16)
        };
        graphics.DrawLines(pulsePen, pulsePoints);

        using var iconHandle = new SafeIconHandle(bitmap.GetHicon());
        return (Icon)Icon.FromHandle(iconHandle.DangerousGetHandle()).Clone();
    }
}
