using System.Linq;
using FairvilleInn.Domain;
using Godot;

namespace FairvilleInn.Presentation.Interactables;

public partial class DoorNode : Interactable
{
    [Export]
    public string DoorName { get; set; } = "door";

    [Export]
    public bool StartsLocked { get; set; }

    private Door _door = null!;
    private Sprite2D _leaf = null!;
    private StaticBody2D _blocker = null!;
    private CollisionPolygon2D _blockerShape = null!;

    public override string Prompt => _door.IsOpen ? $"Close {DoorName}" : $"Open {DoorName}";

    public override float ApproachDistance => 40.0f;

    public override void _Ready()
    {
        base._Ready();
        _door = new Door(DoorName, StartsLocked);
        _leaf = GetNode<Sprite2D>("Leaf");
        _blocker = GetNode<StaticBody2D>("Blocker");
        _blockerShape = _blocker.GetNode<CollisionPolygon2D>("CollisionPolygon2D");
    }

    public override void Interact(Node2D actor)
    {
        if (_door.IsOpen)
        {
            if (Services.CloseDoor.Execute(_door, DoorwayOccupied()) == DoorCloseResult.Closed)
            {
                ApplyState();
            }
        }
        else if (Services.OpenDoor.Execute(_door) == DoorOpenResult.Opened)
        {
            ApplyState();
        }
    }

    private void ApplyState()
    {
        // Door sheet: frame 0 closed, frame 1 open.
        _leaf.Frame = _door.IsOpen ? 1 : 0;
        // Layer 0 both stops physics collisions and excludes the body from navmesh baking.
        _blocker.CollisionLayer = _door.IsOpen ? 0u : 1u;
        RaiseNavigationChanged();
    }

    private bool DoorwayOccupied()
    {
        var shape = new ConvexPolygonShape2D { Points = _blockerShape.Polygon };
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = _blockerShape.GlobalTransform,
            CollideWithBodies = true,
            CollideWithAreas = false,
        };

        // Walls and other static geometry share edges with the doorway; only moving bodies count.
        return GetWorld2D().DirectSpaceState.IntersectShape(query)
            .Select(hit => hit["collider"].As<GodotObject>())
            .Any(collider => collider is CharacterBody2D or RigidBody2D);
    }
}
