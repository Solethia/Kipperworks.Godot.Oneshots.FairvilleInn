using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Godot;
using static FairvilleInn.Tooling.ArtPipeline.Iso;

namespace FairvilleInn.Tooling.ArtPipeline;

// Packs assets/art into runtime assets: tile atlases + TileSet, prop sheets + scenes,
// and tiles.json (asset name -> atlas coords / scene tile id) so the room editor and the
// preview room can refer to assets by name.
//
// Rooms are painted in the Godot editor with the packed TileSet, so slots must be stable:
// a floor keeps its atlas coordinates and a prop keeps its scene-tile id across repacks.
// New assets take the lowest free slot; the previous tiles.json is the source of truth.
public static class Packer
{
    public const int FloorSource = 0;
    public const int WallSource = 1;
    public const int PropsSource = 2;

    private const int AtlasColumns = 8;
    private const string Diamond = "-32, 0, 0, -16, 32, 0, 0, 16";

    public sealed class SceneTile
    {
        public string Scene { get; set; } = "";
        public int Id { get; set; }
    }

    public sealed class Index
    {
        public Dictionary<string, int[]> Floor { get; set; } = [];
        public Dictionary<string, int[]> Wall { get; set; } = [];
        public Dictionary<string, SceneTile> Door { get; set; } = [];
        public Dictionary<string, SceneTile> Prop { get; set; } = [];

        [System.Text.Json.Serialization.JsonIgnore]
        public IEnumerable<KeyValuePair<string, SceneTile>> SceneTiles => Door.Concat(Prop);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public static Index PackAll()
    {
        var problems = new List<string>();
        foreach (var meta in AssetMeta.All())
        {
            problems.AddRange(AssetTypes.Get(meta.Type).Validate(meta).Select(p => $"{meta.Type}/{meta.Name}: {p}"));
        }

        var doors = AssetMeta.All("door");
        var props = AssetMeta.All("prop");
        foreach (var clash in doors.Select(m => m.Name).Intersect(props.Select(m => m.Name)))
        {
            problems.Add($"'{clash}' is both a door and a prop; doors and props share one scene folder and palette");
        }

        if (problems.Count > 0)
        {
            throw new PipelineException("Cannot pack, fix these first:\n" + string.Join("\n", problems));
        }

        var previous = LoadPreviousIndex();
        var index = new Index();
        PackTiles(index, previous);

        var sceneIds = AssignSceneIds(previous, doors.Concat(props).Select(m => m.Name));
        foreach (var meta in doors)
        {
            index.Door[meta.Name] = new SceneTile { Scene = PackDoor(meta), Id = sceneIds[meta.Name] };
        }

        foreach (var meta in props)
        {
            index.Prop[meta.Name] = new SceneTile { Scene = PackProp(meta), Id = sceneIds[meta.Name] };
        }

        WriteTileSet(index);
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

    // Slot assignments from the last pack. Tolerates the pre-scene-tile format, where door
    // and prop entries were plain scene paths (those get fresh ids).
    private static Index LoadPreviousIndex()
    {
        if (!FileAccess.FileExists(Paths.TilesIndexRes))
        {
            return new Index();
        }

        using var doc = JsonDocument.Parse(Files.ReadText(Paths.TilesIndexRes));
        var index = new Index();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            switch (prop.Name)
            {
                case "floor": index.Floor = Coords(prop.Value); break;
                case "wall": index.Wall = Coords(prop.Value); break;
                case "door": index.Door = Scenes(prop.Value); break;
                case "prop": index.Prop = Scenes(prop.Value); break;
            }
        }

        return index;

        static Dictionary<string, int[]> Coords(JsonElement e) =>
            e.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.EnumerateArray().Select(v => v.GetInt32()).ToArray());

        static Dictionary<string, SceneTile> Scenes(JsonElement e) =>
            e.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ValueKind == JsonValueKind.String
                ? new SceneTile { Scene = p.Value.GetString()!, Id = 0 }
                : JsonSerializer.Deserialize<SceneTile>(p.Value.GetRawText(), JsonOptions)!);
    }

    // Keeps previously assigned atlas cells; new names fill the lowest free cell.
    private static Dictionary<string, int[]> AssignCoords(Dictionary<string, int[]> previous, IEnumerable<string> names)
    {
        var coords = new Dictionary<string, int[]>();
        var used = new HashSet<(int, int)>();
        var fresh = new List<string>();
        foreach (var name in names)
        {
            if (previous.TryGetValue(name, out var c) && used.Add((c[0], c[1])))
            {
                coords[name] = c;
            }
            else
            {
                fresh.Add(name);
            }
        }

        var slot = 0;
        foreach (var name in fresh)
        {
            while (used.Contains((slot % AtlasColumns, slot / AtlasColumns)))
            {
                slot++;
            }

            coords[name] = [slot % AtlasColumns, slot / AtlasColumns];
            used.Add((coords[name][0], coords[name][1]));
        }

        return coords;
    }

    // Scene-tile ids start at 1 (0 is not a valid alternative-tile id for scene collections).
    private static Dictionary<string, int> AssignSceneIds(Index previous, IEnumerable<string> names)
    {
        var known = previous.SceneTiles.ToDictionary(kv => kv.Key, kv => kv.Value.Id);
        var ids = new Dictionary<string, int>();
        var used = new HashSet<int>();
        var fresh = new List<string>();
        foreach (var name in names)
        {
            if (known.TryGetValue(name, out var id) && id > 0 && used.Add(id))
            {
                ids[name] = id;
            }
            else
            {
                fresh.Add(name);
            }
        }

        var next = 1;
        foreach (var name in fresh)
        {
            while (used.Contains(next))
            {
                next++;
            }

            ids[name] = next;
            used.Add(next);
        }

        return ids;
    }

    private static Image Atlas(List<AssetMeta> metas, Dictionary<string, int[]> coords, int tileH)
    {
        var maxCol = metas.Count == 0 ? 0 : metas.Max(m => coords[m.Name][0]);
        var maxRow = metas.Count == 0 ? 0 : metas.Max(m => coords[m.Name][1]);
        var atlas = Painter.Blank(TileW * (maxCol + 1), tileH * (maxRow + 1));
        foreach (var meta in metas)
        {
            var (col, row) = (coords[meta.Name][0], coords[meta.Name][1]);
            var tile = Files.LoadPng(Paths.Join(meta.FolderRes, "tile.png"));
            tile.Convert(Image.Format.Rgba8);
            atlas.BlitRect(tile, new Rect2I(Vector2I.Zero, tile.GetSize()), new Vector2I(col * TileW, row * tileH));
        }

        return atlas;
    }

    private static void PackTiles(Index index, Index previous)
    {
        var outDir = Paths.GeneratedRes + "/tilesets";
        var floors = AssetMeta.All("floor");
        var walls = AssetMeta.All("wall");
        index.Floor = AssignCoords(previous.Floor, floors.Select(m => m.Name));
        index.Wall = AssignCoords(previous.Wall, walls.Select(m => m.Name));
        Files.SavePng(Atlas(floors, index.Floor, TileH), $"{outDir}/floors.png");
        Files.SavePng(Atlas(walls, index.Wall, WallH), $"{outDir}/walls.png");
    }

    // One TileSet with three sources: Floors and Walls atlases, plus a Props scene
    // collection holding every door and prop scene so they can be painted like tiles.
    private static void WriteTileSet(Index index)
    {
        var outDir = Paths.GeneratedRes + "/tilesets";
        var sceneTiles = index.SceneTiles.OrderBy(kv => kv.Value.Id).ToList();
        var floorCoords = index.Floor;
        var wallCoords = index.Wall;

        var sb = new StringBuilder();
        sb.AppendLine($"[gd_resource type=\"TileSet\" load_steps={7 + sceneTiles.Count} format=3]").AppendLine()
            .AppendLine($"[ext_resource type=\"Texture2D\" path=\"{outDir}/floors.png\" id=\"1\"]")
            .AppendLine($"[ext_resource type=\"Texture2D\" path=\"{outDir}/walls.png\" id=\"2\"]");
        foreach (var (name, tile) in sceneTiles)
        {
            sb.AppendLine($"[ext_resource type=\"PackedScene\" path=\"{tile.Scene}\" id=\"scene_{name}\"]");
        }

        sb.AppendLine()
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

        sb.AppendLine().AppendLine("[sub_resource type=\"TileSetScenesCollectionSource\" id=\"props\"]")
            .AppendLine("resource_name = \"Props\"");
        foreach (var (name, tile) in sceneTiles)
        {
            sb.AppendLine($"scenes/{tile.Id}/scene = ExtResource(\"scene_{name}\")")
                .AppendLine($"scenes/{tile.Id}/display_placeholder = false");
        }

        sb.AppendLine().AppendLine("[resource]")
            .AppendLine("tile_shape = 1")
            .AppendLine("tile_layout = 5")
            .AppendLine($"tile_size = Vector2i({TileW}, {TileH})")
            .AppendLine("physics_layer_0/collision_layer = 1")
            .AppendLine("navigation_layer_0/layers = 1")
            .AppendLine($"sources/{FloorSource} = SubResource(\"floors\")")
            .AppendLine($"sources/{WallSource} = SubResource(\"walls\")")
            .AppendLine($"sources/{PropsSource} = SubResource(\"props\")");
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
        // The node origin is the centre of the footprint's anchor cell (top-left), which is
        // where a TileMapLayer places a painted scene tile. The sprite and collision are
        // shifted to the footprint centre; y-sorting the root makes the sprite sort there too.
        var anchor = BlockCentre(0, 0, meta.FootprintW, meta.FootprintH) - CellCentre(0, 0);
        // Sprite is centred on its node; shift it up so the footprint diamond centre sits on the node.
        var offsetY = (fh - imageH) / 2f;
        var scene = $"{Paths.GeneratedScenesRes}/props/{meta.Name}.tscn";
        Files.WriteText(scene, string.Join("\n",
            "[gd_scene load_steps=2 format=3]",
            "",
            $"[ext_resource type=\"Texture2D\" path=\"{png}\" id=\"1\"]",
            "",
            $"[node name=\"{NodeName(meta.Name)}\" type=\"StaticBody2D\"]",
            "y_sort_enabled = true",
            "",
            "[node name=\"Sprite\" type=\"Sprite2D\" parent=\".\"]",
            $"position = Vector2({Tscn.Num(anchor.X)}, {Tscn.Num(anchor.Y)})",
            "texture = ExtResource(\"1\")",
            $"offset = Vector2(0, {Tscn.Num(offsetY)})",
            "",
            "[node name=\"CollisionPolygon2D\" type=\"CollisionPolygon2D\" parent=\".\"]",
            $"position = Vector2({Tscn.Num(anchor.X)}, {Tscn.Num(anchor.Y)})",
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
