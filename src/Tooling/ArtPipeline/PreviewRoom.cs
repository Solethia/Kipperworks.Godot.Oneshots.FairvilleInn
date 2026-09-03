using System.Collections.Generic;
using System.Linq;

namespace FairvilleInn.Tooling.ArtPipeline;

// Builds a tiny test room showing one asset in context, at scenes/rooms/_preview.tscn.
public static class PreviewRoom
{
    public static string Build(AssetMeta meta)
    {
        var text = Text(meta);
        Files.WriteText($"{Paths.RoomsRes}/_preview.txt", text);
        Files.WriteText(Paths.PreviewRoomRes, RoomBuilder.Build(RoomBuilder.Parse("_preview", text)));
        return Paths.PreviewRoomRes;
    }

    private static string Text(AssetMeta meta)
    {
        string header;
        List<string> rows;
        switch (meta.Type)
        {
            case "floor":
                header = $"floor x: {meta.Name}";
                rows =
                [
                    "##########",
                    "#xxxxxxxx#",
                    "#xxxxxxxx#",
                    "#xx....xx#",
                    "#xx.P..xx#",
                    "#xx....xx#",
                    "#xxxxxxxx#",
                    "#xxxxxxxx#",
                    "##########",
                ];
                break;
            case "wall":
                header = $"wall #: {meta.Name}";
                rows =
                [
                    "##########",
                    "#........#",
                    "#........#",
                    "#...##...#",
                    "#...#....#",
                    "#...P....#",
                    "#........#",
                    "#........#",
                    "##########",
                ];
                break;
            case "door":
                header = $"door D: {meta.Name}";
                rows =
                [
                    "##########",
                    "#........#",
                    "#........#",
                    "#...P....######",
                    "#........D,,,,#",
                    "#........#,,,,#",
                    "#........######",
                    "#........#",
                    "##########",
                ];
                break;
            case "prop":
            {
                header = $"prop T: {meta.Name}";
                int w = meta.FootprintW, h = meta.FootprintH;
                var size = System.Math.Max(10, w + 6);
                var pad = (size - 2 - w) / 2;
                string Blank() => "#" + new string('.', size - 2) + "#";
                rows = [new string('#', size), Blank(), Blank()];
                for (var i = 0; i < h; i++)
                {
                    rows.Add("#" + new string('.', pad) + new string('T', w) + new string('.', size - 2 - pad - w) + "#");
                }

                rows.Add(Blank());
                rows.Add("#" + new string('.', size / 2 - 1) + "P" + new string('.', size - 2 - size / 2) + "#");
                rows.Add(Blank());
                rows.Add(new string('#', size));
                break;
            }
            default:
                throw new PipelineException($"no preview for type '{meta.Type}'");
        }

        return header + "\n---\n" + string.Join("\n", rows) + "\n";
    }
}
