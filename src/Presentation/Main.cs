using FairvilleInn.Presentation.Composition;
using FairvilleInn.Presentation.Interactables;
using FairvilleInn.Presentation.UI;
using Godot;

namespace FairvilleInn.Presentation;

public partial class Main : Node2D
{
    [Export]
    public PackedScene PlayerScene { get; set; } = null!;

    // Default room; overridden by a `--room=res://...` user argument (used by the art pipeline preview).
    [Export]
    public PackedScene RoomScene { get; set; } = null!;

    private NavigationRegion2D _navigation = null!;

    public override void _Ready()
    {
        _navigation = GetNode<NavigationRegion2D>("Navigation");
        LoadRoom();

        var messenger = GetNode<MessageLabel>("UI/Message");
        var services = new GameServices(messenger, ProjectSettings.GlobalizePath("user://save.json"));

        foreach (var node in GetTree().GetNodesInGroup(Interactable.GroupName))
        {
            if (node is Interactable interactable)
            {
                interactable.Initialize(services);
                interactable.NavigationChanged += RebakeNavigation;
            }
        }

        SpawnPlayer();
        RebakeNavigation();
    }

    private void LoadRoom()
    {
        var scene = RoomScene;
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--room=") && ResourceLoader.Exists(arg[7..]))
            {
                scene = GD.Load<PackedScene>(arg[7..]);
            }
        }

        var room = scene.Instantiate<Node2D>();
        room.Name = "Room";
        AddChild(room);
        MoveChild(room, GetNode("Navigation").GetIndex() + 1);
    }

    private void SpawnPlayer()
    {
        var room = GetNode<Node2D>("Room");
        var player = PlayerScene.Instantiate<Node2D>();
        player.GlobalPosition = room.GetNode<Node2D>("PlayerSpawn").GlobalPosition;
        room.GetNode("Actors").AddChild(player);
    }

    private void RebakeNavigation()
    {
        _navigation.BakeNavigationPolygon(onThread: false);
    }
}
