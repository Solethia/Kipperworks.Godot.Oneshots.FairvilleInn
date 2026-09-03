using Godot;

namespace FairvilleInn.Tooling.ArtPipeline;

// The project's own coloured placeholder set (what the game ships with until real art
// lands). Re-creating them is idempotent; artist-edited PNGs are overwritten only for
// the assets listed here.
public static class ProjectPlaceholders
{
    private sealed record Entry(string Type, string Name, string Display, Palette Palette, int W = 1, int H = 1, int Height = 0);

    private static readonly Entry[] Entries =
    [
        new("floor", "wood", "Wood floor", Palette.FromHex("#66512f", "#4a3a22", "#4a3a22")),
        new("floor", "stone", "Stone floor", Palette.FromHex("#5c5e66", "#43454c", "#43454c")),
        new("floor", "dirt", "Dirt floor", Palette.FromHex("#4a3d2e", "#352b20", "#352b20")),
        new("wall", "plaster", "Plaster wall", Palette.FromHex("#9a8d7a", "#7f7362", "#5f5548")),
        new("wall", "stone", "Stone wall", Palette.FromHex("#77726b", "#615c55", "#4b4741")),
        new("door", "cellar_door", "cellar door", Palette.FromHex("#5a3819", "#3a2410", "#8b6a3e")),
        new("prop", "table", "Table", Palette.FromHex("#6b4a2a", "#8a6339", "#a9804d"), 1, 1, 64),
        new("prop", "table_large", "Large table", Palette.FromHex("#6b4a2a", "#8a6339", "#a9804d"), 2, 2, 32),
    ];

    public static void Generate()
    {
        foreach (var e in Entries)
        {
            var meta = Scaffolder.Create(e.Type, e.Name, e.Display, e.W, e.H, e.Height, overwrite: true);
            var type = AssetTypes.Get(e.Type);
            foreach (var file in type.Files(meta).Keys)
            {
                Files.SavePng(type.Placeholder(meta, file, e.Palette), Paths.Join(meta.FolderRes, file));
            }
        }
    }
}
