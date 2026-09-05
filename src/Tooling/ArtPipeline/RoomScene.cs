using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using static FairvilleInn.Tooling.ArtPipeline.Iso;

namespace FairvilleInn.Tooling.ArtPipeline;

// Writes a room scene in the layout the game expects (see ART_PIPELINE.md, "Rooms").
// Rooms are normally painted in the Godot editor; this is used to scaffold new rooms and
// the preview room. All three tile layers share the packed TileSet: floors and walls are
// atlas tiles, props and doors are scene tiles from the Props source.
public sealed class RoomScene(string name)
{
    private readonly Dictionary<(int X, int Y), (int Ax, int Ay)> _floor = [];
    private readonly Dictionary<(int X, int Y), (int Ax, int Ay)> _walls = [];
    private readonly Dictionary<(int X, int Y), int> _props = [];
    private readonly List<(string Name, string[] Lines, Vector2 Pos)> _visitors = [];
    private Vector2? _spawn;

    // A cell holds either a floor or a wall; the last call wins.
    public void Floor(int x, int y, int[] atlas)
    {
        _walls.Remove((x, y));
        _floor[(x, y)] = (atlas[0], atlas[1]);
    }

    public void Wall(int x, int y, int[] atlas)
    {
        _floor.Remove((x, y));
        _walls[(x, y)] = (atlas[0], atlas[1]);
    }

    // Anchor cell = top-left of the prop's footprint.
    public void Prop(int x, int y, Packer.SceneTile tile) => _props[(x, y)] = tile.Id;

    public void Spawn(int x, int y) => _spawn = CellCentre(x, y);

    public void Visitor(int x, int y, string visitorName, params string[] lines) => _visitors.Add((visitorName, lines, CellCentre(x, y)));

    // Creates scenes/rooms/<name>.tscn as a walled WxH rectangle to paint in the editor.
    public static string Scaffold(string roomName, int width, int height, string floor, string wall)
    {
        if (roomName.Length == 0 || roomName.Any(c => !(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '_')))
        {
            throw new PipelineException($"room name '{roomName}' must be lowercase letters, digits and underscores");
        }

        if (width < 3 || height < 3)
        {
            throw new PipelineException("room must be at least 3x3 (walls included)");
        }

        var scene = $"{Paths.RoomScenesRes}/{roomName}.tscn";
        if (FileAccess.FileExists(scene))
        {
            throw new PipelineException($"{scene} already exists; rooms are edited in the Godot editor, not regenerated");
        }

        var index = Packer.LoadIndex();
        if (!index.Floor.TryGetValue(floor, out var floorTile))
        {
            throw new PipelineException($"no packed floor '{floor}' (have: {string.Join(", ", index.Floor.Keys)})");
        }

        if (!index.Wall.TryGetValue(wall, out var wallTile))
        {
            throw new PipelineException($"no packed wall '{wall}' (have: {string.Join(", ", index.Wall.Keys)})");
        }

        var room = new RoomScene(roomName);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    room.Wall(x, y, wallTile);
                }
                else
                {
                    room.Floor(x, y, floorTile);
                }
            }
        }

        room.Spawn(width / 2, height / 2);
        Files.WriteText(scene, room.Write());
        return scene;
    }

    public string Write()
    {
        if (_spawn is null)
        {
            throw new PipelineException($"{name}: room needs a player spawn");
        }

        var sb = new StringBuilder();
        sb.AppendLine("[gd_scene load_steps=4 format=3]").AppendLine()
            .AppendLine($"[ext_resource type=\"TileSet\" path=\"{Paths.TileSetRes}\" id=\"tileset\"]")
            .AppendLine("[ext_resource type=\"Script\" path=\"res://src/Presentation/World/OccludingWallLayer.cs\" id=\"walls_script\"]")
            .AppendLine("[ext_resource type=\"PackedScene\" path=\"res://scenes/characters/visitor.tscn\" id=\"visitor\"]").AppendLine()
            .AppendLine($"[node name=\"{name}\" type=\"Node2D\" groups=[\"navigation_source\"]]")
            .AppendLine("y_sort_enabled = true").AppendLine()
            .AppendLine("[node name=\"Floor\" type=\"TileMapLayer\" parent=\".\"]")
            .AppendLine("z_index = -1")
            .AppendLine("tile_set = ExtResource(\"tileset\")")
            .AppendLine($"tile_map_data = {TileMapData(_floor.Select(c => (c.Key.X, c.Key.Y, Packer.FloorSource, c.Value.Ax, c.Value.Ay, 0)))}")
            // Tiles only feed the baked NavigationRegion2D; they must not register their own regions.
            .AppendLine("navigation_enabled = false").AppendLine()
            .AppendLine("[node name=\"Walls\" type=\"TileMapLayer\" parent=\".\"]")
            .AppendLine("y_sort_enabled = true")
            .AppendLine("tile_set = ExtResource(\"tileset\")")
            .AppendLine($"tile_map_data = {TileMapData(_walls.Select(c => (c.Key.X, c.Key.Y, Packer.WallSource, c.Value.Ax, c.Value.Ay, 0)))}")
            .AppendLine("script = ExtResource(\"walls_script\")").AppendLine()
            // Scene tiles: atlas coords are always (0,0); the alternative id selects the scene.
            .AppendLine("[node name=\"Props\" type=\"TileMapLayer\" parent=\".\"]")
            .AppendLine("y_sort_enabled = true")
            .AppendLine("tile_set = ExtResource(\"tileset\")")
            .AppendLine($"tile_map_data = {TileMapData(_props.Select(c => (c.Key.X, c.Key.Y, Packer.PropsSource, 0, 0, c.Value)))}")
            .AppendLine("navigation_enabled = false").AppendLine()
            .AppendLine("[node name=\"PlayerSpawn\" type=\"Marker2D\" parent=\".\"]")
            .AppendLine($"position = {Vec(_spawn.Value)}").AppendLine()
            .AppendLine("[node name=\"Actors\" type=\"Node2D\" parent=\".\"]")
            .AppendLine("y_sort_enabled = true");

        for (var i = 0; i < _visitors.Count; i++)
        {
            var (vname, lines, pos) = _visitors[i];
            sb.AppendLine().AppendLine($"[node name=\"Visitor{i + 1}\" parent=\"Actors\" instance=ExtResource(\"visitor\")]")
                .AppendLine($"position = {Vec(pos)}")
                .AppendLine($"VisitorName = {Tscn.Str(vname)}")
                .AppendLine("Lines = PackedStringArray(" + string.Join(", ", lines.Select(Tscn.Str)) + ")");
        }

        return sb.ToString();
    }

    // TileMapLayer.tile_map_data, format 0: per cell x, y (int16), source, atlas x, atlas y, alternative (uint16).
    private static string TileMapData(IEnumerable<(int X, int Y, int Source, int Ax, int Ay, int Alt)> cells)
    {
        var bytes = new List<byte> { 0, 0 };
        foreach (var (x, y, source, ax, ay, alt) in cells)
        {
            bytes.AddRange(System.BitConverter.GetBytes((short)x));
            bytes.AddRange(System.BitConverter.GetBytes((short)y));
            bytes.AddRange(System.BitConverter.GetBytes((ushort)source));
            bytes.AddRange(System.BitConverter.GetBytes((ushort)ax));
            bytes.AddRange(System.BitConverter.GetBytes((ushort)ay));
            bytes.AddRange(System.BitConverter.GetBytes((ushort)alt));
        }

        return "PackedByteArray(" + string.Join(", ", bytes) + ")";
    }

    private static string Vec(Vector2 v) => $"Vector2({Tscn.Num(v.X)}, {Tscn.Num(v.Y)})";
}
