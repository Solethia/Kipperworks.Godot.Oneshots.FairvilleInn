using FairvilleInn.Domain;
using FairvilleInn.Presentation.Characters;
using Godot;

namespace FairvilleInn.Presentation.Interactables;

public partial class VisitorNode : Interactable
{
    [Export]
    public string VisitorName { get; set; } = "Visitor";

    [Export]
    public string[] Lines { get; set; } =
    [
        "Evening, traveller. Take a seat by the fire.",
        "Odd noises from the cellar lately. Nobody wants to look.",
    ];

    private Visitor _visitor = null!;
    private DirectionalSprite _sprite = null!;

    public override string Prompt => $"Talk to {VisitorName}";

    public override float ApproachDistance => 48.0f;

    public override void _Ready()
    {
        base._Ready();
        _visitor = new Visitor(VisitorName, Lines);
        _sprite = GetNode<DirectionalSprite>("Sprite");
        _sprite.SetFacing(Vector2.Down);
    }

    public override void Interact(Node2D actor)
    {
        _sprite.SetFacing(GlobalPosition.DirectionTo(actor.GlobalPosition));
        Services.TalkToVisitor.Execute(_visitor);
    }
}
