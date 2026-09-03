using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace FairvilleInn.Tooling.ArtPipeline;

// One asset instance: assets/art/<Type>/<Name>/meta.json plus its PNGs.
public sealed class AssetMeta
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int[] Footprint { get; set; } = [1, 1];
    public int Height { get; set; }

    [JsonIgnore]
    public int FootprintW => Footprint[0];

    [JsonIgnore]
    public int FootprintH => Footprint[1];

    [JsonIgnore]
    public string FolderRes => Paths.AssetFolder(Type, Name);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public void Save()
    {
        DirAccess.MakeDirRecursiveAbsolute(FolderRes);
        Files.WriteText(Paths.Join(FolderRes, "meta.json"), JsonSerializer.Serialize(this, JsonOptions) + "\n");
    }

    public static AssetMeta? Load(string folderRes)
    {
        var path = Paths.Join(folderRes, "meta.json");
        if (!FileAccess.FileExists(path))
        {
            return null;
        }

        return JsonSerializer.Deserialize<AssetMeta>(Files.ReadText(path), JsonOptions);
    }

    public static List<AssetMeta> All(string? type = null)
    {
        var result = new List<AssetMeta>();
        foreach (var typeDir in Files.Subdirs(Paths.ArtRes))
        {
            if (type is not null && typeDir != type)
            {
                continue;
            }

            foreach (var name in Files.Subdirs($"{Paths.ArtRes}/{typeDir}"))
            {
                var meta = Load(Paths.AssetFolder(typeDir, name));
                if (meta is not null)
                {
                    result.Add(meta);
                }
            }
        }

        return result;
    }
}

// A kind of art the game knows how to consume: which PNGs it consists of, how big
// they are, and how to draw a placeholder and a guide overlay for each.
public abstract class AssetType
{
    public abstract string Key { get; }
    public abstract string Description { get; }
    public virtual bool HasFootprint => false;
    public abstract IReadOnlyDictionary<string, Vector2I> Files(AssetMeta meta);
    public abstract Image Placeholder(AssetMeta meta, string file, Palette palette);

    public Image Placeholder(AssetMeta meta, string file) => Placeholder(meta, file, Palette.Template);
    public abstract Image Guide(AssetMeta meta, string file);

    public List<string> Validate(AssetMeta meta)
    {
        var problems = new List<string>();
        foreach (var (file, size) in Files(meta))
        {
            var path = Paths.Join(meta.FolderRes, file);
            if (!FileAccess.FileExists(path))
            {
                problems.Add($"missing {file}");
                continue;
            }

            var img = Image.LoadFromFile(Paths.Global(path));
            if (img.GetSize() != size)
            {
                problems.Add($"{file} is {img.GetWidth()}x{img.GetHeight()}, expected {size.X}x{size.Y}");
            }
        }

        return problems;
    }
}

// Flat colours for placeholder art: main face, shaded face, top/light face.
public readonly record struct Palette(Color Fill, Color Dark, Color Light)
{
    public static readonly Palette Template = new(Painter.TemplateFill, Painter.TemplateDark, Painter.TemplateLight);

    public static Palette FromHex(string fill, string dark, string light) => new(new Color(fill), new Color(dark), new Color(light));
}

public static class AssetTypes
{
    public static readonly IReadOnlyList<AssetType> All =
    [
        new Types.FloorType(), new Types.WallType(), new Types.DoorType(), new Types.PropType(),
    ];

    public static AssetType Get(string key)
    {
        foreach (var type in All)
        {
            if (type.Key == key)
            {
                return type;
            }
        }

        throw new KeyNotFoundException($"unknown asset type '{key}'");
    }
}
