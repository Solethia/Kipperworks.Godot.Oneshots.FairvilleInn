using Godot;

namespace FairvilleInn.Presentation.Characters;

// Drives an 8-direction sprite sheet: one row per facing (S, SW, W, NW, N, NE, E, SE),
// Hframes columns of animation. Frame 0 is the idle pose.
public partial class DirectionalSprite : Sprite2D
{
    private const int DirectionCount = 8;

    [Export]
    public float FramesPerSecond { get; set; } = 8.0f;

    private int _row;
    private int _column;
    private bool _moving;
    private double _elapsed;

    public void SetFacing(Vector2 direction)
    {
        if (direction.LengthSquared() < 0.001f)
        {
            return;
        }

        // Screen angle: 0° = east, 90° = south. Row 0 is south, rows advance clockwise on screen.
        var degrees = Mathf.RadToDeg(Mathf.Atan2(direction.Y, direction.X));
        var fromSouth = Mathf.PosMod(degrees - 90.0f, 360.0f);
        _row = Mathf.RoundToInt(fromSouth / 45.0f) % DirectionCount;
        ApplyFrame();
    }

    public void SetMoving(bool moving)
    {
        if (_moving == moving)
        {
            return;
        }

        _moving = moving;
        _column = 0;
        _elapsed = 0;
        ApplyFrame();
    }

    public override void _Process(double delta)
    {
        if (!_moving || Hframes <= 1)
        {
            return;
        }

        _elapsed += delta;
        var frameTime = 1.0 / FramesPerSecond;
        while (_elapsed >= frameTime)
        {
            _elapsed -= frameTime;
            _column = (_column + 1) % Hframes;
        }

        ApplyFrame();
    }

    private void ApplyFrame()
    {
        Frame = _row * Hframes + _column;
    }
}
