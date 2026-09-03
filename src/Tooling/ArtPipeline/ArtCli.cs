using System;
using System.Linq;
using Godot;

namespace FairvilleInn.Tooling.ArtPipeline;

// Headless entry point:  godot --headless --path . tools/art_cli.tscn -- <command> [args]
//   placeholders                       regenerate the project's coloured placeholder assets
//   new <type> <name> [display] [WxH] [height]
//   pack                               assets/art -> assets/generated + scenes/generated, rebuild rooms
//   rooms                              rebuild scenes/rooms/*.tscn from rooms/*.txt
//   preview <type> <name>              pack + write scenes/rooms/_preview.tscn
//   validate
public partial class ArtCli : Node
{
    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        var code = 0;
        try
        {
            Run(args);
        }
        catch (Exception e) when (e is PipelineException or ArgumentException or InvalidOperationException)
        {
            GD.PrintErr(e.Message);
            code = 1;
        }

        GetTree().Quit(code);
    }

    private static void Run(string[] args)
    {
        var cmd = args.Length > 0 ? args[0] : "help";
        switch (cmd)
        {
            case "placeholders":
                ProjectPlaceholders.Generate();
                GD.Print("placeholders written to assets/art");
                break;
            case "new":
            {
                var size = args.Length > 4 ? args[4].Split('x') : ["1", "1"];
                var meta = Scaffolder.Create(args[1], args[2], args.Length > 3 ? args[3] : null,
                    int.Parse(size[0]), int.Parse(size.Length > 1 ? size[1] : size[0]),
                    args.Length > 5 ? int.Parse(args[5]) : 0);
                GD.Print(Paths.Global(meta.FolderRes));
                break;
            }
            case "pack":
                Packer.PackAll();
                GD.Print(string.Join("\n", RoomBuilder.BuildAll()));
                GD.Print("packed");
                break;
            case "rooms":
                GD.Print(string.Join("\n", RoomBuilder.BuildAll()));
                break;
            case "preview":
            {
                Packer.PackAll();
                RoomBuilder.BuildAll();
                var meta = AssetMeta.All(args[1]).FirstOrDefault(m => m.Name == args[2])
                    ?? throw new PipelineException($"no asset {args[1]}/{args[2]}");
                GD.Print(PreviewRoom.Build(meta));
                break;
            }
            case "validate":
            {
                var problems = AssetMeta.All()
                    .SelectMany(m => AssetTypes.Get(m.Type).Validate(m).Select(p => $"{m.Type}/{m.Name}: {p}"))
                    .ToList();
                if (problems.Count > 0)
                {
                    throw new PipelineException(string.Join("\n", problems));
                }

                GD.Print("all assets valid");
                break;
            }
            default:
                GD.Print("commands: placeholders | new <type> <name> [display] [WxH] [height] | pack | rooms | preview <type> <name> | validate");
                break;
        }
    }
}
