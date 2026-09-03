using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FairvilleInn.Tooling.ArtPipeline;

// Thin helpers over Godot's FileAccess/DirAccess using res:// paths.
public static class Files
{
    public static string ReadText(string resPath)
    {
        using var file = FileAccess.Open(resPath, FileAccess.ModeFlags.Read)
            ?? throw new System.IO.IOException($"cannot open {resPath}: {FileAccess.GetOpenError()}");
        return file.GetAsText();
    }

    public static void WriteText(string resPath, string text)
    {
        var dir = resPath[..resPath.LastIndexOf('/')];
        DirAccess.MakeDirRecursiveAbsolute(dir);
        using var file = FileAccess.Open(resPath, FileAccess.ModeFlags.Write)
            ?? throw new System.IO.IOException($"cannot write {resPath}: {FileAccess.GetOpenError()}");
        file.StoreString(text);
    }

    public static void SavePng(Image image, string resPath)
    {
        var dir = resPath[..resPath.LastIndexOf('/')];
        DirAccess.MakeDirRecursiveAbsolute(dir);
        var err = image.SavePng(resPath);
        if (err != Error.Ok)
        {
            throw new System.IO.IOException($"cannot save {resPath}: {err}");
        }
    }

    public static Image LoadPng(string resPath) => Image.LoadFromFile(Paths.Global(resPath));

    public static IEnumerable<string> Subdirs(string resPath)
    {
        using var dir = DirAccess.Open(resPath);
        return dir is null ? [] : dir.GetDirectories().OrderBy(d => d).ToList();
    }

    public static IEnumerable<string> FilesIn(string resPath, string extension)
    {
        using var dir = DirAccess.Open(resPath);
        return dir is null
            ? []
            : dir.GetFiles().Where(f => f.EndsWith(extension)).OrderBy(f => f).ToList();
    }
}
