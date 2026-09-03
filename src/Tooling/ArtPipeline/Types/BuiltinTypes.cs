using System.Collections.Generic;
using Godot;
using static FairvilleInn.Tooling.ArtPipeline.Iso;
using static FairvilleInn.Tooling.ArtPipeline.Painter;

namespace FairvilleInn.Tooling.ArtPipeline.Types;

public sealed class FloorType : AssetType
{
    public override string Key => "floor";
    public override string Description => "Floor tile, 64x32 diamond filling the image. Walkable.";

    public override IReadOnlyDictionary<string, Vector2I> Files(AssetMeta meta) =>
        new Dictionary<string, Vector2I> { ["tile.png"] = new(TileW, TileH) };

    public override Image Placeholder(AssetMeta meta, string file, Palette palette)
    {
        var img = Blank(TileW, TileH);
        Polygon(img, Diamond(TileW / 2f, TileH / 2f, TileW, TileH), palette.Fill);
        return img;
    }

    public override Image Guide(AssetMeta meta, string file)
    {
        var img = Blank(TileW, TileH);
        var d = Diamond(TileW / 2f, TileH / 2f, TileW, TileH);
        Polygon(img, d, GuideFill);
        Outline(img, d, GuideLine);
        return img;
    }
}

public sealed class WallType : AssetType
{
    public override string Key => "wall";
    public override string Description => "Wall tile, 64x96: footprint diamond in the bottom 32px, 64px of height. Blocks movement, fades when occluding.";

    public override IReadOnlyDictionary<string, Vector2I> Files(AssetMeta meta) =>
        new Dictionary<string, Vector2I> { ["tile.png"] = new(TileW, WallH) };

    public override Image Placeholder(AssetMeta meta, string file, Palette palette)
    {
        var img = Blank(TileW, WallH);
        const int rise = WallH - TileH;
        float cx = TileW / 2f, midY = WallH - TileH / 2f, botY = WallH;
        Polygon(img, [new(0, midY), new(cx, botY), new(cx, botY - rise), new(0, midY - rise)], palette.Fill);
        Polygon(img, [new(cx, botY), new(TileW, midY), new(TileW, midY - rise), new(cx, botY - rise)], palette.Dark);
        Polygon(img, Diamond(cx, midY - rise, TileW, TileH), palette.Light);
        return img;
    }

    public override Image Guide(AssetMeta meta, string file)
    {
        var img = Blank(TileW, WallH);
        var d = Footprint(1, 1, WallH);
        Polygon(img, d, GuideFill);
        Outline(img, d, GuideLine);
        Line(img, new Vector2(0, WallH - TileH), new Vector2(TileW, WallH - TileH), GuideLine);
        Anchor(img, TileW / 2, WallH - TileH / 2);
        return img;
    }
}

public sealed class DoorType : AssetType
{
    public override string Key => "door";
    public override string Description => "Door standing on a wall tile: closed.png + open.png, 128x96 each, footprint diamond centred at the bottom.";

    public override IReadOnlyDictionary<string, Vector2I> Files(AssetMeta meta) =>
        new Dictionary<string, Vector2I> { ["closed.png"] = new(DoorW, WallH), ["open.png"] = new(DoorW, WallH) };

    // Thin slab on the wall centre line. Closed runs along map +y; open swings 90° into +x.
    public override Image Placeholder(AssetMeta meta, string file, Palette palette)
    {
        var isOpen = file == "open.png";
        var img = Blank(DoorW, WallH);
        float ox = DoorW / 2f, oy = WallH - TileH / 2f - 1;
        const float height = 54;
        Vector2 P(Vector2 v, float h = 0) => new(Mathf.Round(ox + v.X), Mathf.Round(oy + v.Y - h));

        var hinge = new Vector2(16, -8);
        var (far, thick) = isOpen
            ? (new Vector2(48, 8), new Vector2(-4, 2))
            : (new Vector2(-16, 8), new Vector2(4, 2));
        var a = hinge;
        var b = far;
        var a2 = a + thick;
        var b2 = b + thick;
        // Visible end cap is the one facing the camera (lower on screen).
        var (e, e2) = b.Y > a.Y ? (b, b2) : (a, a2);

        Polygon(img, [P(a2), P(b2), P(b2, height), P(a2, height)], palette.Fill);
        Polygon(img, [P(a, height), P(b, height), P(b2, height), P(a2, height)], palette.Light);
        Polygon(img, [P(e), P(e2), P(e2, height), P(e, height)], palette.Dark);
        return img;
    }

    public override Image Guide(AssetMeta meta, string file)
    {
        var img = Blank(DoorW, WallH);
        float cx = DoorW / 2f, cy = WallH - TileH / 2f;
        var d = Diamond(cx, cy, TileW, TileH);
        Polygon(img, d, GuideFill);
        Outline(img, d, GuideLine);
        // Wall centre line the door sits on (map +y = screen down-left).
        Line(img, new Vector2(cx + 16, cy - 8), new Vector2(cx - 16, cy + 8), GuideAnchor, 2);
        Anchor(img, (int)cx, (int)cy);
        return img;
    }
}

public sealed class PropType : AssetType
{
    public override string Key => "prop";
    public override string Description => "Static obstacle with a WxH tile footprint (e.g. 2x2 table). sprite.png = (W*64) x (H*32 + height).";
    public override bool HasFootprint => true;

    public static Vector2I Size(AssetMeta meta) =>
        new(meta.FootprintW * TileW, meta.FootprintH * TileH + meta.Height);

    public override IReadOnlyDictionary<string, Vector2I> Files(AssetMeta meta) =>
        new Dictionary<string, Vector2I> { ["sprite.png"] = Size(meta) };

    public override Image Placeholder(AssetMeta meta, string file, Palette palette)
    {
        var size = Size(meta);
        var img = Blank(size.X, size.Y);
        float fw = meta.FootprintW * TileW, fh = meta.FootprintH * TileH;
        var rise = Mathf.Max(meta.Height - 6, 8);
        const float inset = 8;
        float cx = fw / 2, midY = size.Y - fh / 2, botY = size.Y - 1 - inset / 2;
        float left = inset, right = fw - inset - 1;
        Polygon(img, [new(left, midY), new(cx, botY), new(cx, botY - rise), new(left, midY - rise)], palette.Fill);
        Polygon(img, [new(cx, botY), new(right, midY), new(right, midY - rise), new(cx, botY - rise)], palette.Dark);
        Polygon(img, [new(cx, size.Y - fh + inset / 2 - rise), new(right, midY - rise), new(cx, botY - rise), new(left, midY - rise)], palette.Light);
        return img;
    }

    public override Image Guide(AssetMeta meta, string file)
    {
        var size = Size(meta);
        var img = Blank(size.X, size.Y);
        var d = Footprint(meta.FootprintW, meta.FootprintH, size.Y);
        Polygon(img, d, GuideFill);
        Outline(img, d, GuideLine);
        var top = size.Y - meta.FootprintH * TileH;
        Line(img, new Vector2(0, top), new Vector2(size.X, top), GuideLine);
        Anchor(img, size.X / 2, size.Y - meta.FootprintH * TileH / 2);
        return img;
    }
}
