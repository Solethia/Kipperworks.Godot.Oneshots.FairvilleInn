using System.Collections.Generic;
using System.Linq;

namespace FairvilleInn.Tooling.ArtPipeline;

// Builds a tiny test room showing one asset in context, at scenes/rooms/_preview.tscn.
public static class PreviewRoom
{
    public static string Build(AssetMeta meta)
    {
        var index = Packer.LoadIndex();
        var room = new RoomScene("_preview");
        var floor = Pick(index.Floor, "wood", meta.Type == "floor" ? null : meta.Name);
        var wall = Pick(index.Wall, "plaster", meta.Type == "wall" ? meta.Name : null);

        switch (meta.Type)
        {
            case "floor":
            {
                // The tested floor fills the room, with an island of the default floor for contrast.
                Box(room, 0, 0, 10, 9, index.Floor[meta.Name], wall);
                for (var y = 3; y <= 5; y++)
                {
                    for (var x = 3; x <= 6; x++)
                    {
                        room.Floor(x, y, floor);
                    }
                }

                room.Spawn(4, 4);
                break;
            }
            case "wall":
                Box(room, 0, 0, 10, 9, floor, index.Wall[meta.Name]);
                room.Wall(4, 3, index.Wall[meta.Name]);
                room.Wall(5, 3, index.Wall[meta.Name]);
                room.Wall(4, 4, index.Wall[meta.Name]);
                room.Spawn(4, 5);
                break;
            case "door":
            {
                // Main room plus a stone side room to the east; the door sits in the shared wall.
                Box(room, 0, 0, 10, 9, floor, wall);
                Box(room, 9, 3, 6, 4, Pick(index.Floor, "stone", null), wall);
                room.Floor(9, 4, floor);
                room.Prop(9, 4, index.Door[meta.Name]);
                room.Spawn(4, 3);
                break;
            }
            case "prop":
            {
                int w = meta.FootprintW, h = meta.FootprintH;
                var size = System.Math.Max(10, w + 6);
                var pad = (size - 2 - w) / 2;
                Box(room, 0, 0, size, h + 6, floor, wall);
                room.Prop(1 + pad, 3, index.Prop[meta.Name]);
                room.Spawn(size / 2, h + 4);
                break;
            }
            default:
                throw new PipelineException($"no preview for type '{meta.Type}'");
        }

        Files.WriteText(Paths.PreviewRoomRes, room.Write());
        return Paths.PreviewRoomRes;
    }

    // Walled rectangle with a floor inside; overlapping boxes share walls, later floors win.
    private static void Box(RoomScene room, int x0, int y0, int w, int h, int[] floor, int[] wall)
    {
        for (var y = y0; y < y0 + h; y++)
        {
            for (var x = x0; x < x0 + w; x++)
            {
                var edge = x == x0 || y == y0 || x == x0 + w - 1 || y == y0 + h - 1;
                if (edge)
                {
                    room.Wall(x, y, wall);
                }
                else
                {
                    room.Floor(x, y, floor);
                }
            }
        }
    }

    private static int[] Pick(Dictionary<string, int[]> table, string preferred, string? avoid)
    {
        if (table.TryGetValue(preferred, out var v) && preferred != avoid)
        {
            return v;
        }

        return table.Where(kv => kv.Key != avoid).Select(kv => kv.Value).FirstOrDefault()
            ?? throw new PipelineException("preview needs at least one packed tile besides the one being previewed");
    }
}
