using Godot;

namespace FairvilleInn.Tooling.ArtPipeline;

// Project layout the pipeline reads from and writes to.
public static class Paths
{
    public const string ArtRes = "res://assets/art";                 // artist-owned sources (.gdignore'd)
    public const string GeneratedRes = "res://assets/generated";     // packed runtime assets
    public const string GeneratedScenesRes = "res://scenes/generated";
    public const string RoomScenesRes = "res://scenes/rooms";
    public const string TileSetRes = GeneratedRes + "/tilesets/inn.tres";
    public const string TilesIndexRes = GeneratedRes + "/tilesets/tiles.json";
    public const string PreviewRoomRes = RoomScenesRes + "/_preview.tscn";

    public static string Global(string resPath) => ProjectSettings.GlobalizePath(resPath);

    public static string AssetFolder(string type, string name) => $"{ArtRes}/{type}/{name}";

    public static string Join(string resPath, string file) => resPath.TrimEnd('/') + "/" + file;
}
