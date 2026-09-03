using FairvilleInn.Application.Ports;
using FairvilleInn.Domain;

namespace FairvilleInn.Application.UseCases;

public sealed class TalkToVisitorUseCase
{
    private readonly IPlayerMessenger _messenger;

    public TalkToVisitorUseCase(IPlayerMessenger messenger)
    {
        _messenger = messenger;
    }

    public void Execute(Visitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        _messenger.Show($"{visitor.Name}: \"{visitor.Speak()}\"");
    }
}
