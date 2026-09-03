using FairvilleInn.Presentation.Composition;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FairvilleInn.Presentation.Interactables;

public abstract partial class Interactable : Area2D
{
    public const string GroupName = "interactables";

    // Raised when the interactable changes the walkable area (e.g. a door opens).
    public event Action? NavigationChanged;

    protected GameServices Services { get; private set; } = null!;

    public abstract string Prompt { get; }

    // How far from this interactable an actor should stop before interacting (world px).
    // Zero means walk right up to it.
    public virtual float ApproachDistance => 0.0f;

    // Where an actor coming from `from` should stand to interact. Isometric ground
    // distances are foreshortened vertically, so the stand-off is an ellipse.
    public Vector2 ApproachPoint(Vector2 from)
    {
        if (ApproachDistance <= 0.0f)
        {
            return GlobalPosition;
        }

        var offset = from - GlobalPosition;
        var direction = offset.LengthSquared() < 0.001f ? Vector2.Down : offset.Normalized();
        return GlobalPosition + new Vector2(direction.X, direction.Y * 0.5f) * ApproachDistance;
    }

    public override void _Ready()
    {
        AddToGroup(GroupName);
    }

    public void Initialize(GameServices services)
    {
        Services = services;
    }

    public abstract void Interact(Node2D actor);

    // True when the point lands on any of this interactable's sprites (their frame rect).
    public bool CoversPoint(Vector2 worldPosition)
    {
        return Sprites().Any(sprite =>
            sprite.GetRect().HasPoint(sprite.ToLocal(worldPosition)));
    }

    // Screen depth used to pick the front-most interactable when several overlap.
    public float SortY => GlobalPosition.Y;

    public void SetHighlighted(Material? outline)
    {
        foreach (var sprite in Sprites())
        {
            sprite.Material = outline;
        }
    }

    public static Interactable? FindAt(SceneTree tree, Vector2 worldPosition)
    {
        return tree.GetNodesInGroup(GroupName)
            .OfType<Interactable>()
            .Where(i => i.IsVisibleInTree() && i.CoversPoint(worldPosition))
            .OrderByDescending(i => i.SortY)
            .FirstOrDefault();
    }

    private IEnumerable<Sprite2D> Sprites()
    {
        return GetChildren().OfType<Sprite2D>().Where(s => s.Texture is not null);
    }

    protected void RaiseNavigationChanged()
    {
        NavigationChanged?.Invoke();
    }
}
