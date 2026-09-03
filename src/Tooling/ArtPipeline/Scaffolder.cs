using System.Text;
using System.Text.RegularExpressions;

namespace FairvilleInn.Tooling.ArtPipeline;

public static class Scaffolder
{
    public static string Slug(string text)
    {
        var slug = Regex.Replace(text.Trim().ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
        return slug.Length == 0 ? throw new System.ArgumentException("name needs letters or digits") : slug;
    }

    // Creates assets/art/<type>/<name>/ with placeholder PNGs, guide overlays, meta.json and a README.
    public static AssetMeta Create(string typeKey, string name, string? displayName = null,
        int footprintW = 1, int footprintH = 1, int height = 0, bool overwrite = false)
    {
        var type = AssetTypes.Get(typeKey);
        var slug = Slug(name);
        var meta = new AssetMeta
        {
            Type = typeKey,
            Name = slug,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? slug.Replace('_', ' ') : displayName.Trim(),
            Footprint = [footprintW, footprintH],
            Height = height > 0 ? height : (type.HasFootprint ? 64 : 0),
        };

        if (!overwrite && Godot.DirAccess.DirExistsAbsolute(meta.FolderRes))
        {
            throw new System.InvalidOperationException($"{meta.FolderRes} already exists");
        }

        meta.Save();
        var readme = new StringBuilder()
            .AppendLine($"{meta.DisplayName}  ({typeKey})")
            .AppendLine()
            .AppendLine(type.Description)
            .AppendLine()
            .AppendLine("Files:");
        foreach (var (file, size) in type.Files(meta))
        {
            Files.SavePng(type.Placeholder(meta, file), Paths.Join(meta.FolderRes, file));
            Files.SavePng(type.Guide(meta, file), Paths.Join(meta.FolderRes, "_guide_" + file));
            readme.AppendLine($"  {file}  {size.X}x{size.Y}");
        }

        readme.AppendLine()
            .AppendLine("_guide_*.png are reference overlays (magenta = footprint diamond, cyan = anchor);")
            .AppendLine("they are never used in-game. Paint over the placeholder PNGs, keep the exact size,")
            .AppendLine("keep everything outside the artwork transparent, then press Test in the Art Pipeline window.");
        Files.WriteText(Paths.Join(meta.FolderRes, "README.txt"), readme.ToString());
        return meta;
    }
}
