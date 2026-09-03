using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Godot;
using static FairvilleInn.Tooling.ArtPipeline.Iso;

namespace FairvilleInn.Tooling.ArtPipeline;

// Packs assets/art into runtime assets: tile atlases + TileSet, prop sheets + scenes,
// and tiles.json (asset name -> atlas coords / scene path) that RoomBuilder consumes.
public static class Packer
{
    private const int AtlasColumns = 8;
    private const string Diamond = "-32, 0, 0, -16, 32, 0, 0, 16";

    public sealed class Index
    {
        public Dictionary<string, int[]> Floor { get; set; } = [];
        public Dictionary<string, int[]> Wall { get; set; } = [];
        public Dictionary<string, string> Door { get; set; } = [];
        public Dictionary<string, string> Prop { get; set; } = [];
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public static Index PackAll()
    {
        var problems = new List<string>();
        foreach (var meta in AssetMeta.All())
        {
            problems.AddRange(AssetTypes.Get(meta.Type).Validate(meta).Select(p => $"{meta.Type}/{meta.Name}: {p}"));
        }

        if (problems.Count > 0)
        {
            throw new PipelineException("Cannot pack, fix these first:\n" + string.Join("\n", problems));
        }

        var index = new Index();
        PackTiles(index);
        foreach (var meta in AssetMeta.All("door"))
        {
            index.Door[meta.Name] = PackDoor(meta);
        }

        foreach (var meta in AssetMeta.All("prop"))
        {
            index.Prop[meta.Name] = PackProp(meta);
        }

        Files.WriteText(Paths.TilesIndexRes, JsonSerializer.Serialize(index, JsonOptions) + "\n");
        return index;
    }

    public static Index LoadIndex()
    {
        if (!FileAccess.FileExists(Paths.TilesIndexRes))
        {
            throw new PipelineException("tiles.json missing — pack first");
        }

        return JsonSerializer.Deserialize<Index>(Files.ReadText(Paths.TilesIndexRes), JsonOptions)!;
    }

    private static (Image atlas, Dictionary<string, int[]> coords) Atlas(List<AssetMeta> metas, int tileH)
    {
        var count = Mathf.Max(metas.Count, 1);
        var rows = (count + AtlasColumns - 1) / AtlasColumns;
        var atlas = Painter.Blank(TileW * Mathf.Min(count, AtlasColumns), tileH * rows);
        var coords = new Dictionary<string, int[]>();
        for (var i = 0; i < metas.Count; i++)
        {
            int col = i % AtlasColumns, row = i / AtlasColumns;
            var tile = Files.LoadPng(Paths.Join(metas[i].FolderRes, "tile.png"));
            tile.Convert(Image.Format.Rgba8);
            atlas.BlitRect(tile, new Rect2I(Vector2I.Zero, tile.GetSize()), new Vector2I(col * TileW, row * tileH));
            coords[metas[i].Name] = [col, row];
        }

        return (atlas, coords);
    }

    private static void PackTiles(Index index)
    {
        var outDir = Paths.GeneratedRes + "/tilesets";
        var (floorImg, floorCoords) = Atlas(AssetMeta.All("floor"), TileH);
        var (wallImg, wallCoords) = Atlas(AssetMeta.All("wall"), WallH);
        index.Floor = floorCoords;
        index.Wall = wallCoords;
        Files.SavePng(floorImg, $"{outDir}/floors.png");
        Files.SavePng(wallImg, $"{outDir}/walls.png");

        var sb = new StringBuilder();
        sb.AppendLine("[gd_resource type=\"TileSet\" load_steps=6 format=3]").AppendLine()
            .AppendLine($"[ext_resource type=\"Texture2D\" path=\"{outDir}/floors.png\" id=\"1\"]")
            .AppendLine($"[ext_resource type=\"Texture2D\" path=\"{outDir}/walls.png\" id=\"2\"]").AppendLine()
            .AppendLine("[sub_resource type=\"NavigationPolygon\" id=\"nav_diamond\"]")
            .AppendLine($"vertices = PackedVector2Array({Diamond})")
            .AppendLine("polygons = Array[PackedInt32Array]([PackedInt32Array(0, 1, 2, 3)])")
            .AppendLine($"outlines = Array[PackedVector2Array]([PackedVector2Array({Diamond})])").AppendLine()
            .AppendLine("[sub_resource type=\"TileSetAtlasSource\" id=\"floors\"]")
            .AppendLine("resource_name = \"Floors\"")
            .AppendLine("texture = ExtResource(\"1\")")
            .AppendLine($"texture_region_size = Vector2i({TileW}, {TileH})");
        foreach (var (c, r) in floorCoords.Values.Select(v => (v[0], v[1])))
        {
            sb.AppendLine($"{c}:{r}/0 = 0").AppendLine($"{c}:{r}/0/navigation_layer_0/polygon = SubResource(\"nav_diamond\")");
        }

        sb.AppendLine().AppendLine("[sub_resource type=\"TileSetAtlasSource\" id=\"walls\"]")
            .AppendLine("resource_name = \"Walls\"")
            .AppendLine("texture = ExtResource(\"2\")")
            .AppendLine($"texture_region_size = Vector2i({TileW}, {WallH})");
        foreach (var (c, r) in wallCoords.Values.Select(v => (v[0], v[1])))
        {
            sb.AppendLine($"{c}:{r}/0 = 0")
                .AppendLine($"{c}:{r}/0/texture_origin = Vector2i(0, 32)")
                .AppendLine($"{c}:{r}/0/y_sort_origin = 8")
                .AppendLine($"{c}:{r}/0/physics_layer_0/polygon_0/points = PackedVector2Array({Diamond})");
        }

        sb.AppendLine().AppendLine("[resource]")
            .AppendLine("tile_shape = 1")
            .AppendLine("tile_layout = 5")
            .AppendLine($"tile_size = Vector2i({TileW}, {TileH})")
            .AppendLine("physics_layer_0/collision_layer = 1")
            .AppendLine("navigation_layer_0/layers = 1")
            .AppendLine("sources/0 = SubResource(\"floors\")")
            .AppendLine("sources/1 = SubResource(\"walls\")");
        Files.WriteText(Paths.TileSetRes, sb.ToString());
    }

    private static string PackDoor(AssetMeta meta)
    {
        var sheet = Painter.Blank(DoorW * 2, WallH);
        var frames = new[] { "closed.png", "open.png" };
        for (var i = 0; i < frames.Length; i++)
        {
            var frame = Files.LoadPng(Paths.Join(meta.FolderRes, frames[i]));
            frame.Convert(Image.Format.Rgba8);
            sheet.BlitRect(frame, new Rect2I(Vector2I.Zero, frame.GetSize()), new Vector2I(i * DoorW, 0));
        }

        var png = $"{Paths.GeneratedRes}/props/{meta.Name}.png";
        Files.SavePng(sheet, png);

        var scene = $"{Paths.GeneratedScenesRes}/props/{meta.Name}.tscn";
        Files.WriteText(scene, string.Join("\n",
            "[gd_scene load_steps=4 format=3]",
            "",
            "[ext_resource type=\"Script\" path=\"res://src/Presentation/Interactables/DoorNode.cs\" id=\"1\"]",
            $"[ext_resource type=\"Texture2D\" path=\"{png}\" id=\"2\"]",
            "",
            "[sub_resource type=\"CircleShape2D\" id=\"trigger\"]",
            "radius = 40.0",
            "",
            $"[node name=\"{NodeName(meta.Name)}\" type=\"Area2D\"]",
            "script = ExtResource(\"1\")",
            $"DoorName = {Tscn.Str(meta.DisplayName)}",
            "",
            "[node name=\"Leaf\" type=\"Sprite2D\" parent=\".\"]",
            "texture = ExtResource(\"2\")",
            $"offset = Vector2(0, {-(WallH - TileH) / 2})",
            "hframes = 2",
            "",
            "[node name=\"CollisionShape2D\" type=\"CollisionShape2D\" parent=\".\"]",
            "shape = SubResource(\"trigger\")",
            "",
            "[node name=\"Blocker\" type=\"StaticBody2D\" parent=\".\"]",
            "",
            "[node name=\"CollisionPolygon2D\" type=\"CollisionPolygon2D\" parent=\"Blocker\"]",
            $"polygon = PackedVector2Array({Diamond})",
            ""));
        return scene;
    }

    private static string PackProp(AssetMeta meta)
    {
        var png = $"{Paths.GeneratedRes}/props/{meta.Name}.png";
        var sprite = Files.LoadPng(Paths.Join(meta.FolderRes, "sprite.png"));
        Files.SavePng(sprite, png);

        int fw = meta.FootprintW * TileW, fh = meta.FootprintH * TileH;
        var imageH = fh + meta.Height;
        // Sprite is centred on the node; shift it up so the footprint diamond centre sits on the origin.
        var offsetY = (fh - imageH) / 2f;
        var scene = $"{Paths.GeneratedScenesRes}/props/{meta.Name}.tscn";
        Files.WriteText(scene, string.Join("\n",
            "[gd_scene load_steps=2 format=3]",
            "",
            $"[ext_resource type=\"Texture2D\" path=\"{png}\" id=\"1\"]",
            "",
            $"[node name=\"{NodeName(meta.Name)}\" type=\"StaticBody2D\"]",
            "",
            "[node name=\"Sprite\" type=\"Sprite2D\" parent=\".\"]",
            "texture = ExtResource(\"1\")",
            $"offset = Vector2(0, {Tscn.Num(offsetY)})",
            "",
            "[node name=\"CollisionPolygon2D\" type=\"CollisionPolygon2D\" parent=\".\"]",
            $"polygon = PackedVector2Array({-fw / 2}, 0, 0, {-fh / 2}, {fw / 2}, 0, 0, {fh / 2})",
            ""));
        return scene;
    }

    public static string NodeName(string assetName) =>
        string.Concat(assetName.Split('_').Select(p => p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
}

public sealed class PipelineException(string message) : System.Exception(message);

internal static class Tscn
{
    public static string Str(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    public static string Num(float value) => value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}
