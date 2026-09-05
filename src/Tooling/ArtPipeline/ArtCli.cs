using System;
using System.Linq;
using Godot;

namespace FairvilleInn.Tooling.ArtPipeline;

// Headless entry point:  godot --headless --path . tools/art_cli.tscn -- <command> [args]
//   placeholders                       regenerate the project's coloured placeholder assets
//   new <type> <name> [display] [WxH] [height]
//   pack                               assets/art -> assets/generated + scenes/generated
//   room <name> [WxH] [floor] [wall]   scaffold scenes/rooms/<name>.tscn to paint in the editor
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
        catch (Exception e)
        {
            // Headless runs must always quit, otherwise a crash leaves the process hanging.
            GD.PrintErr(e.ToString());
            code = 2;
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
                const string usage = "usage: new <type> <name> [display] [WxH] [height]";
                if (args.Length is < 3 or > 6)
                {
                    throw new PipelineException(usage);
                }

                var (w, h) = args.Length > 4 ? ParseSize(args[4], usage) : (1, 1);
                var height = args.Length > 5 ? ParseInt(args[5], "height", usage) : 0;
                var meta = Scaffolder.Create(args[1], args[2], args.Length > 3 ? args[3] : null, w, h, height);
                GD.Print(Paths.Global(meta.FolderRes));
                break;
            }
            case "pack":
                Packer.PackAll();
                GD.Print("packed");
                break;
            case "room":
            {
                const string usage = "usage: room <name> [WxH] [floor] [wall]";
                if (args.Length is < 2 or > 5)
                {
                    throw new PipelineException(usage);
                }

                var (w, h) = args.Length > 2 ? ParseSize(args[2], usage) : (12, 10);
                GD.Print(RoomScene.Scaffold(args[1], w, h, args.Length > 3 ? args[3] : "wood", args.Length > 4 ? args[4] : "plaster"));
                break;
            }
            case "preview":
            {
                if (args.Length != 3)
                {
                    throw new PipelineException("usage: preview <type> <name>");
                }

                Packer.PackAll();
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
                GD.Print("commands: placeholders | new <type> <name> [display] [WxH] [height] | pack | room <name> [WxH] [floor] [wall] | preview <type> <name> | validate");
                break;
        }
    }

    private static (int W, int H) ParseSize(string text, string usage)
    {
        var parts = text.Split('x');
        if (parts.Length is < 1 or > 2)
        {
            throw new PipelineException($"bad size '{text}': expected W or WxH\n{usage}");
        }

        var w = ParseInt(parts[0], "size", usage);
        var h = parts.Length > 1 ? ParseInt(parts[1], "size", usage) : w;
        return (w, h);
    }

    private static int ParseInt(string text, string what, string usage)
    {
        if (!int.TryParse(text, out var value) || value < 0)
        {
            throw new PipelineException($"bad {what} '{text}': expected a non-negative integer\n{usage}");
        }

        return value;
    }
}
