using FairvilleInn.Presentation.Interactables;
using Godot;

namespace FairvilleInn.Presentation.World;

// Hover feedback for interactables: outline the sprite, switch to a pointing-hand
// cursor and float the interaction prompt above it.
public partial class HoverHighlighter : Node2D
{
    [Export]
    public Shader OutlineShader { get; set; } = null!;

    [Export]
    public Color OutlineColor { get; set; } = new(1.0f, 0.9f, 0.4f);

    [Export]
    public float OutlineThickness { get; set; } = 2.0f;

    // Screen offset of the label from the interactable's ground point.
    [Export]
    public Vector2 LabelOffset { get; set; } = new(0, -110);

    private ShaderMaterial _outline = null!;
    private Label _label = null!;
    private Interactable? _hovered;

    public Interactable? Hovered => _hovered;

    public override void _Ready()
    {
        _outline = new ShaderMaterial { Shader = OutlineShader };
        _outline.SetShaderParameter("line_color", OutlineColor);
        _outline.SetShaderParameter("thickness", OutlineThickness);

        _label = new Label
        {
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            ZIndex = 100,
        };
        _label.AddThemeColorOverride("font_color", Colors.White);
        _label.AddThemeColorOverride("font_outline_color", Colors.Black);
        _label.AddThemeConstantOverride("outline_size", 4);
        AddChild(_label);
    }

    public override void _Process(double delta)
    {
        var target = Interactable.FindAt(GetTree(), GetGlobalMousePosition());
        if (target != _hovered)
        {
            _hovered?.SetHighlighted(null);
            _hovered = target;
            _hovered?.SetHighlighted(_outline);
            Input.SetDefaultCursorShape(_hovered is null
                ? Input.CursorShape.Arrow
                : Input.CursorShape.PointingHand);
        }

        if (_hovered is null)
        {
            _label.Visible = false;
            return;
        }

        _label.Text = _hovered.Prompt;
        _label.Visible = _label.Text.Length > 0;
        _label.ResetSize();
        _label.GlobalPosition = _hovered.GlobalPosition + LabelOffset - new Vector2(_label.Size.X / 2, 0);
    }
}
