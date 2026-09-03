using FairvilleInn.Application.Ports;
using FairvilleInn.Application.UseCases;
using FairvilleInn.Infrastructure.Persistence;

namespace FairvilleInn.Presentation.Composition;

// Composition root: the only place that knows both the ports and their concrete implementations.
public sealed class GameServices
{
    public GameServices(IPlayerMessenger messenger, string savePath)
    {
        OpenDoor = new OpenDoorUseCase(messenger);
        CloseDoor = new CloseDoorUseCase(messenger);
        TalkToVisitor = new TalkToVisitorUseCase(messenger);
        SaveGame = new SaveGameUseCase(new JsonSaveGame(savePath));
    }

    public OpenDoorUseCase OpenDoor { get; }

    public CloseDoorUseCase CloseDoor { get; }

    public TalkToVisitorUseCase TalkToVisitor { get; }

    public SaveGameUseCase SaveGame { get; }
}
