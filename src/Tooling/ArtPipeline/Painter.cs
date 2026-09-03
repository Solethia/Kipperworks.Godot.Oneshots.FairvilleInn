using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FairvilleInn.Tooling.ArtPipeline;

// Isometric grid constants shared by every asset type.
public static class Iso
{
    public const int TileW = 64;
    public const int TileH = 32;
    public const int WallH = 96;
    public const int DoorW = TileW * 2;

    public static Vector2 CellCentre(int x, int y) =>
        new((x - y) * TileW / 2f + TileW / 2f, (x + y) * TileH / 2f + TileH / 2f);

    public static Vector2 BlockCentre(int x, int y, int w, int h)
    {
        var c = CellCentre(x, y);
        return new Vector2(c.X + (w - h) * TileW / 4f, c.Y + (w + h - 2) * TileH / 4f);
    }

    // Diamond centred at (cx, cy) spanning exactly w x h pixels. With pixel-centre
    // sampling, 64x32 diamonds offset by (32,16) tile the plane with no gaps or overlaps.
    public static Vector2[] Diamond(float cx, float cy, float w, float h) =>
    [
        new(cx - w / 2, cy), new(cx, cy - h / 2), new(cx + w / 2, cy), new(cx, cy + h / 2),
    ];

    // Footprint diamond of a WxH-tile object sitting at the bottom of an image.
    public static Vector2[] Footprint(int w, int h, int imageH)
    {
        float fw = w * TileW, fh = h * TileH;
        return Diamond(fw / 2, imageH - fh / 2, fw, fh);
    }
}

// Minimal flat-colour rasteriser on top of Godot.Image.
public static class Painter
{
    public static readonly Color GuideLine = new(1, 0, 1, 0.8f);
    public static readonly Color GuideFill = new(1, 0, 1, 0.15f);
    public static readonly Color GuideAnchor = new(0, 0.8f, 1, 1);
    public static readonly Color TemplateFill = Color.Color8(150, 150, 150);
    public static readonly Color TemplateDark = Color.Color8(110, 110, 110);
    public static readonly Color TemplateLight = Color.Color8(185, 185, 185);

    public static Image Blank(int w, int h)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);
        return img;
    }

    // Scanline fill of a convex or concave simple polygon (pixel centres inside).
    public static void Polygon(Image img, IReadOnlyList<Vector2> points, Color fill)
    {
        var minY = Math.Max(0, (int)MathF.Floor(points.Min(p => p.Y)));
        var maxY = Math.Min(img.GetHeight() - 1, (int)MathF.Ceiling(points.Max(p => p.Y)));
        var xs = new List<float>();
        for (var y = minY; y <= maxY; y++)
        {
            var sy = y + 0.5f;
            xs.Clear();
            for (var i = 0; i < points.Count; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Count];
                if ((sy >= a.Y && sy < b.Y) || (sy >= b.Y && sy < a.Y))
                {
                    xs.Add(a.X + (sy - a.Y) / (b.Y - a.Y) * (b.X - a.X));
                }
            }

            xs.Sort();
            for (var i = 0; i + 1 < xs.Count; i += 2)
            {
                // Fill pixels whose centres lie inside the span.
                var x0 = Math.Max(0, (int)MathF.Ceiling(xs[i] - 0.5f));
                var x1 = Math.Min(img.GetWidth() - 1, (int)MathF.Floor(xs[i + 1] - 0.5f));
                for (var x = x0; x <= x1; x++)
                {
                    Blend(img, x, y, fill);
                }
            }
        }
    }

    public static void Outline(Image img, IReadOnlyList<Vector2> points, Color color)
    {
        for (var i = 0; i < points.Count; i++)
        {
            Line(img, points[i], points[(i + 1) % points.Count], color);
        }
    }

    public static void Line(Image img, Vector2 a, Vector2 b, Color color, int width = 1)
    {
        var steps = (int)MathF.Ceiling(Math.Max(MathF.Abs(b.X - a.X), MathF.Abs(b.Y - a.Y)));
        for (var i = 0; i <= steps; i++)
        {
            var p = steps == 0 ? a : a.Lerp(b, i / (float)steps);
            for (var dx = 0; dx < width; dx++)
            {
                for (var dy = 0; dy < width; dy++)
                {
                    Blend(img, (int)MathF.Round(p.X) + dx, (int)MathF.Round(p.Y) + dy, color);
                }
            }
        }
    }

    public static void Anchor(Image img, int x, int y)
    {
        Line(img, new Vector2(x - 4, y), new Vector2(x + 4, y), GuideAnchor);
        Line(img, new Vector2(x, y - 4), new Vector2(x, y + 4), GuideAnchor);
    }

    private static void Blend(Image img, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= img.GetWidth() || y >= img.GetHeight())
        {
            return;
        }

        img.SetPixel(x, y, color.A >= 1 ? color : img.GetPixel(x, y).Blend(color));
    }
}
