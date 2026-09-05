using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FairvilleInn.Presentation.World;

// Drives occluder fading each frame: wall tiles via OccludingWallLayer, props via their sprite.
public partial class OcclusionFader : Node2D
{
    [Export]
    public NodePath WallsPath { get; set; } = null!;

    [Export]
    public NodePath PropsPath { get; set; } = null!;

    [Export]
    public float FadedAlpha { get; set; } = 0.4f;

    [Export]
    public float FadeSpeed { get; set; } = 6.0f;

    // Body-sized box used to decide whether an occluder actually covers the player.
    [Export]
    public Rect2 SubjectBox { get; set; } = new(-24, -100, 48, 110);

    // Occluders within this radius of the cursor fade, so you can see what you're pointing at.
    [Export]
    public float HoverRadius { get; set; } = 40.0f;

    private OccludingWallLayer? _walls;
    private Node? _props;

    public override void _Process(double delta)
    {
        // The room is instantiated by Main at runtime, so bind on first use.
        _walls ??= GetNodeOrNull<OccludingWallLayer>(WallsPath);
        _props ??= GetNodeOrNull<Node>(PropsPath);

        if (GetTree().GetFirstNodeInGroup(Player.GroupName) is not Node2D player)
        {
            return;
        }

        var subject = new Rect2(player.GlobalPosition + SubjectBox.Position, SubjectBox.Size);
        var hover = GetGlobalMousePosition();

        _walls?.UpdateOcclusion(subject, player.GlobalPosition.Y, hover, HoverRadius);
        FadeProps(subject, player.GlobalPosition.Y, (float)delta);
    }

    private void FadeProps(Rect2 subject, float subjectSortY, float delta)
    {
        if (_props is null)
        {
            return;
        }

        foreach (var prop in _props.GetChildren().OfType<Node2D>())
        {
            var sprite = prop.GetChildren().OfType<Sprite2D>().FirstOrDefault();
            if (sprite is null)
            {
                continue;
            }

            var rect = sprite.GlobalTransform * sprite.GetRect();
            // Multi-tile props anchor on their top-left cell; the sprite sits at the footprint centre, which is what y-sorts.
            var covers = sprite.GlobalPosition.Y > subjectSortY && rect.Intersects(subject);
            var target = covers ? FadedAlpha : 1.0f;
            var colour = sprite.Modulate;
            colour.A = Mathf.MoveToward(colour.A, target, delta * FadeSpeed);
            sprite.Modulate = colour;
        }
    }
}
