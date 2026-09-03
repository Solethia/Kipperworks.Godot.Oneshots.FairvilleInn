using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using static FairvilleInn.Tooling.ArtPipeline.Iso;

namespace FairvilleInn.Tooling.ArtPipeline;

// ASCII room (.txt) -> Godot scene (.tscn), resolving tiles and props by asset name.
//
// Header keys (before a `---` line):
//   floor <char>: <floor asset>          (defaults: '.' wood, ',' stone)
//   wall <char>: <wall asset>            (default: '#' plaster)
//   door <char>: <door asset> [| display name]
//   prop <char>: <prop asset>            (footprint from the asset's meta.json)
//   visitor <char>: <name> | line 1 | line 2 | ...
// Map: 'P' = player spawn, space = void. Doors, visitors, props and 'P' stand on the
// '.' floor. Multi-tile props are drawn as a full WxH block of their char, anchored at
// the block's top-left cell.
public static class RoomBuilder
{
    private const int FloorSource = 0;
    private const int WallSource = 1;

    public sealed class Room
    {
        public string Name = "";
        public Dictionary<char, string> Floors = new() { ['.'] = "wood", [','] = "stone" };
        public Dictionary<char, string> Walls = new() { ['#'] = "plaster" };
        public Dictionary<char, (string Asset, string? Display)> Doors = [];
        public Dictionary<char, string> Props = [];
        public Dictionary<char, (string Name, string[] Lines)> Visitors = [];
        public List<string> Rows = [];
    }

    public static Room Parse(string name, string text)
    {
        var room = new Room { Name = name };
        text = text.Replace("\r\n", "\n");
        var split = text.IndexOf("\n---\n", System.StringComparison.Ordinal);
        var header = split < 0 ? "" : text[..split];
        var body = split < 0 ? text : text[(split + 5)..];

        foreach (var raw in header.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var colon = line.IndexOf(':');
            if (colon < 0)
            {
                throw new PipelineException($"{name}: bad header line '{line}'");
            }

            var keyParts = line[..colon].Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var value = line[(colon + 1)..].Trim();
            if (keyParts[0] == "tileset")
            {
                continue; // legacy: the packed tileset is always used
            }

            if (keyParts.Length < 2 || keyParts[1].Length != 1)
            {
                throw new PipelineException($"{name}: header key '{keyParts[0]}' needs a single map character");
            }

            var ch = keyParts[1][0];
            var parts = value.Split('|').Select(p => p.Trim()).ToArray();
            switch (keyParts[0])
            {
                case "floor": room.Floors[ch] = value; break;
                case "wall": room.Walls[ch] = value; break;
                case "door": room.Doors[ch] = (parts[0], parts.Length > 1 ? parts[1] : null); break;
                case "prop": room.Props[ch] = value.Split(' ')[0]; break;
                case "visitor":
                    if (parts.Length < 2 || parts[0].Length == 0 || parts.Skip(1).Any(l => l.Length == 0))
                    {
                        throw new PipelineException(
                            $"{name}: visitor '{ch}' needs a name and at least one dialogue line: 'visitor {ch}: <name> | <line> [| <line>...]'");
                    }

                    room.Visitors[ch] = (parts[0], parts[1..]);
                    break;
                default: throw new PipelineException($"{name}: unknown header key '{keyParts[0]}'");
            }
        }

        room.Rows = body.Split('\n').Where(r => r.Trim('\n').Length > 0).Select(r => r.TrimEnd()).ToList();
        return room;
    }

    public static string Build(Room room)
    {
        var index = Packer.LoadIndex();
        var footprints = AssetMeta.All("prop").ToDictionary(m => m.Name, m => (m.FootprintW, m.FootprintH));

        int[] Tile(Dictionary<string, int[]> table, string kind, string asset) =>
            table.TryGetValue(asset, out var v) ? v : throw new PipelineException($"{room.Name}: no packed {kind} '{asset}'");
        string Scene(Dictionary<string, string> table, string kind, string asset) =>
            table.TryGetValue(asset, out var v) ? v : throw new PipelineException($"{room.Name}: no packed {kind} '{asset}'");

        var defaultFloor = Tile(index.Floor, "floor", room.Floors['.']);
        var floor = new List<(int, int, int, int, int)>();
        var walls = new List<(int, int, int, int, int)>();
        Vector2? spawn = null;
        var doors = new List<(string Asset, string? Display, Vector2 Pos)>();
        var props = new List<(string Asset, Vector2 Pos)>();
        var visitors = new List<(string Name, string[] Lines, Vector2 Pos)>();
        var consumed = new HashSet<(int, int)>();

        for (var y = 0; y < room.Rows.Count; y++)
        {
            var row = room.Rows[y];
            for (var x = 0; x < row.Length; x++)
            {
                var ch = row[x];
                if (ch == ' ')
                {
                    continue;
                }

                if (room.Walls.TryGetValue(ch, out var wall))
                {
                    var t = Tile(index.Wall, "wall", wall);
                    walls.Add((x, y, WallSource, t[0], t[1]));
                    continue;
                }

                if (room.Floors.TryGetValue(ch, out var fl))
                {
                    var t = Tile(index.Floor, "floor", fl);
                    floor.Add((x, y, FloorSource, t[0], t[1]));
                    continue;
                }

                floor.Add((x, y, FloorSource, defaultFloor[0], defaultFloor[1]));
                var pos = CellCentre(x, y);
                if (ch == 'P')
                {
                    spawn = pos;
                }
                else if (room.Doors.TryGetValue(ch, out var door))
                {
                    doors.Add((door.Asset, door.Display, pos));
                }
                else if (room.Visitors.TryGetValue(ch, out var visitor))
                {
                    visitors.Add((visitor.Name, visitor.Lines, pos));
                }
                else if (room.Props.TryGetValue(ch, out var prop))
                {
                    if (consumed.Contains((x, y)))
                    {
                        continue;
                    }

                    if (!footprints.TryGetValue(prop, out var fp))
                    {
                        throw new PipelineException($"{room.Name}: unknown prop asset '{prop}'");
                    }

                    for (var dy = 0; dy < fp.FootprintH; dy++)
                    {
                        for (var dx = 0; dx < fp.FootprintW; dx++)
                        {
                            var r = y + dy < room.Rows.Count ? room.Rows[y + dy] : "";
                            if (x + dx >= r.Length || r[x + dx] != ch)
                            {
                                throw new PipelineException($"{room.Name}: prop '{ch}' at {x},{y} needs a full {fp.FootprintW}x{fp.FootprintH} block");
                            }

                            consumed.Add((x + dx, y + dy));
                        }
                    }

                    props.Add((prop, BlockCentre(x, y, fp.FootprintW, fp.FootprintH)));
                }
                else
                {
                    throw new PipelineException($"{room.Name}: unknown map char '{ch}' at {x},{y}");
                }
            }
        }

        if (spawn is null)
        {
            throw new PipelineException($"{room.Name}: map needs a 'P' player spawn");
        }

        var doorScenes = doors.Select(d => d.Asset).Distinct().ToDictionary(a => a, a => Scene(index.Door, "door", a));
        var propScenes = props.Select(p => p.Asset).Distinct().ToDictionary(a => a, a => Scene(index.Prop, "prop", a));

        var sb = new StringBuilder();
        sb.AppendLine($"[gd_scene load_steps={3 + doorScenes.Count + propScenes.Count} format=3]").AppendLine()
            .AppendLine($"[ext_resource type=\"TileSet\" path=\"{Paths.TileSetRes}\" id=\"tileset\"]")
            .AppendLine("[ext_resource type=\"Script\" path=\"res://src/Presentation/World/OccludingWallLayer.cs\" id=\"walls_script\"]")
            .AppendLine("[ext_resource type=\"PackedScene\" path=\"res://scenes/characters/visitor.tscn\" id=\"visitor\"]");
        foreach (var (asset, path) in doorScenes)
        {
            sb.AppendLine($"[ext_resource type=\"PackedScene\" path=\"{path}\" id=\"door_{asset}\"]");
        }

        foreach (var (asset, path) in propScenes)
        {
            sb.AppendLine($"[ext_resource type=\"PackedScene\" path=\"{path}\" id=\"prop_{asset}\"]");
        }

        sb.AppendLine()
            .AppendLine($"[node name=\"{room.Name}\" type=\"Node2D\" groups=[\"navigation_source\"]]")
            .AppendLine("y_sort_enabled = true").AppendLine()
            .AppendLine("[node name=\"Floor\" type=\"TileMapLayer\" parent=\".\"]")
            .AppendLine("z_index = -1")
            .AppendLine("tile_set = ExtResource(\"tileset\")")
            .AppendLine($"tile_map_data = {TileMapData(floor)}")
            // Tiles only feed the baked NavigationRegion2D; they must not register their own regions.
            .AppendLine("navigation_enabled = false").AppendLine()
            .AppendLine("[node name=\"Walls\" type=\"TileMapLayer\" parent=\".\"]")
            .AppendLine("y_sort_enabled = true")
            .AppendLine("tile_set = ExtResource(\"tileset\")")
            .AppendLine($"tile_map_data = {TileMapData(walls)}")
            .AppendLine("script = ExtResource(\"walls_script\")").AppendLine()
            .AppendLine("[node name=\"PlayerSpawn\" type=\"Marker2D\" parent=\".\"]")
            .AppendLine($"position = {Vec(spawn.Value)}").AppendLine()
            .AppendLine("[node name=\"Props\" type=\"Node2D\" parent=\".\"]")
            .AppendLine("y_sort_enabled = true");

        for (var i = 0; i < doors.Count; i++)
        {
            var (asset, display, pos) = doors[i];
            sb.AppendLine().AppendLine($"[node name=\"Door{i + 1}\" parent=\"Props\" instance=ExtResource(\"door_{asset}\")]")
                .AppendLine($"position = {Vec(pos)}");
            if (display is not null)
            {
                sb.AppendLine($"DoorName = {Tscn.Str(display)}");
            }
        }

        for (var i = 0; i < props.Count; i++)
        {
            var (asset, pos) = props[i];
            sb.AppendLine().AppendLine($"[node name=\"{Packer.NodeName(asset)}{i + 1}\" parent=\"Props\" instance=ExtResource(\"prop_{asset}\")]")
                .AppendLine($"position = {Vec(pos)}");
        }

        sb.AppendLine().AppendLine("[node name=\"Actors\" type=\"Node2D\" parent=\".\"]").AppendLine("y_sort_enabled = true");
        for (var i = 0; i < visitors.Count; i++)
        {
            var (vname, lines, pos) = visitors[i];
            sb.AppendLine().AppendLine($"[node name=\"Visitor{i + 1}\" parent=\"Actors\" instance=ExtResource(\"visitor\")]")
                .AppendLine($"position = {Vec(pos)}")
                .AppendLine($"VisitorName = {Tscn.Str(vname)}")
                .AppendLine("Lines = PackedStringArray(" + string.Join(", ", lines.Select(Tscn.Str)) + ")");
        }

        return sb.ToString();
    }

    // Compiles rooms/<name>.txt to scenes/rooms/<name>.tscn and returns the scene path.
    public static string BuildFile(string roomTxtRes)
    {
        var name = roomTxtRes[(roomTxtRes.LastIndexOf('/') + 1)..].Replace(".txt", "");
        var scene = $"{Paths.RoomScenesRes}/{name}.tscn";
        Files.WriteText(scene, Build(Parse(name, Files.ReadText(roomTxtRes))));
        return scene;
    }

    public static List<string> BuildAll()
    {
        return Files.FilesIn(Paths.RoomsRes, ".txt")
            .Where(f => !f.StartsWith('_'))
            .Select(f => BuildFile($"{Paths.RoomsRes}/{f}"))
            .ToList();
    }

    private static string TileMapData(List<(int X, int Y, int Source, int Ax, int Ay)> cells)
    {
        var bytes = new List<byte> { 0, 0 }; // format version
        foreach (var (x, y, source, ax, ay) in cells)
        {
            bytes.AddRange(System.BitConverter.GetBytes((short)x));
            bytes.AddRange(System.BitConverter.GetBytes((short)y));
            bytes.AddRange(System.BitConverter.GetBytes((ushort)source));
            bytes.AddRange(System.BitConverter.GetBytes((ushort)ax));
            bytes.AddRange(System.BitConverter.GetBytes((ushort)ay));
            bytes.AddRange(System.BitConverter.GetBytes((ushort)0));
        }

        return "PackedByteArray(" + string.Join(", ", bytes) + ")";
    }

    private static string Vec(Vector2 v) => $"Vector2({Tscn.Num(v.X)}, {Tscn.Num(v.Y)})";
}
