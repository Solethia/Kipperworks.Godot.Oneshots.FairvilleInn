using FairvilleInn.Application.Ports;
using Godot;

namespace FairvilleInn.Presentation.UI;

public partial class MessageLabel : Label, IPlayerMessenger
{
    [Export]
    public float VisibleSeconds { get; set; } = 4.0f;

    private Timer _hideTimer = null!;

    public override void _Ready()
    {
        _hideTimer = new Timer { OneShot = true };
        AddChild(_hideTimer);
        _hideTimer.Timeout += () => Text = string.Empty;
        Text = string.Empty;
    }

    public void Show(string message)
    {
        Text = message;
        _hideTimer.Start(VisibleSeconds);
    }
}
