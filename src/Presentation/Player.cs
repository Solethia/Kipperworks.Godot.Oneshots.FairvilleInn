using System.Collections.Generic;
using System.Linq;
using FairvilleInn.Presentation.Characters;
using FairvilleInn.Presentation.Interactables;
using Godot;

namespace FairvilleInn.Presentation;

public partial class Player : CharacterBody2D
{
    public const string GroupName = "player";

    [Export]
    public float MoveSpeed { get; set; } = 180.0f;

    private readonly List<Interactable> _nearby = [];
    private NavigationAgent2D _agent = null!;
    private DirectionalSprite _sprite = null!;
    private Node2D? _clickMarker;
    private Interactable? _pendingInteraction;

    public override void _Ready()
    {
        AddToGroup(GroupName);
        _agent = GetNode<NavigationAgent2D>("NavigationAgent2D");
        _sprite = GetNode<DirectionalSprite>("Sprite");
        _clickMarker = GetTree().GetFirstNodeInGroup("click_marker") as Node2D;
        _sprite.SetFacing(Vector2.Down);

        var range = GetNode<Area2D>("InteractionRange");
        range.AreaEntered += area =>
        {
            if (area is Interactable interactable)
            {
                _nearby.Add(interactable);
            }
        };
        range.AreaExited += area =>
        {
            if (area is Interactable interactable)
            {
                _nearby.Remove(interactable);
            }
        };
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            return;
        }

        var clickPosition = GetGlobalMousePosition();
        var clicked = InteractableAt(clickPosition);

        if (clicked is not null && _nearby.Contains(clicked))
        {
            // Already in reach: interact immediately, no walking needed.
            _pendingInteraction = null;
            _sprite.SetFacing(GlobalPosition.DirectionTo(clicked.GlobalPosition));
            clicked.Interact(this);
            return;
        }

        _pendingInteraction = clicked;
        MoveTo(clicked?.ApproachPoint(GlobalPosition) ?? clickPosition);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_pendingInteraction is not null && _nearby.Contains(_pendingInteraction) && ReachedApproach(_pendingInteraction))
        {
            var target = _pendingInteraction;
            _pendingInteraction = null;
            StopMoving();
            _sprite.SetFacing(GlobalPosition.DirectionTo(target.GlobalPosition));
            target.Interact(this);
        }

        if (_agent.IsNavigationFinished())
        {
            StopMoving();
        }
        else
        {
            var next = _agent.GetNextPathPosition();
            Velocity = GlobalPosition.DirectionTo(next) * MoveSpeed;
            _sprite.SetFacing(Velocity);
            _sprite.SetMoving(true);
        }

        MoveAndSlide();
    }

    private bool ReachedApproach(Interactable target)
    {
        // Arrived at the stand-off point, or close enough that the path can't get closer.
        return _agent.IsNavigationFinished()
            || GlobalPosition.DistanceTo(target.GlobalPosition) <= target.ApproachDistance + 16.0f;
    }

    private void MoveTo(Vector2 target)
    {
        _agent.TargetPosition = target;
        if (_clickMarker is not null)
        {
            _clickMarker.GlobalPosition = target;
            _clickMarker.Visible = true;
        }
    }

    private void StopMoving()
    {
        Velocity = Vector2.Zero;
        // Cancel any remaining path so the next frame doesn't resume walking.
        _agent.TargetPosition = GlobalPosition;
        _sprite.SetMoving(false);
        if (_clickMarker is not null)
        {
            _clickMarker.Visible = false;
        }
    }

    private Interactable? InteractableAt(Vector2 worldPosition)
    {
        // Prefer the sprite the cursor visibly sits on (matches the hover highlight),
        // fall back to the trigger area around the interactable's feet.
        var onSprite = Interactable.FindAt(GetTree(), worldPosition);
        if (onSprite is not null)
        {
            return onSprite;
        }

        var query = new PhysicsPointQueryParameters2D
        {
            Position = worldPosition,
            CollideWithAreas = true,
            CollideWithBodies = false,
        };

        return GetWorld2D().DirectSpaceState.IntersectPoint(query)
            .Select(hit => hit["collider"].As<GodotObject>())
            .OfType<Interactable>()
            .FirstOrDefault();
    }
}
