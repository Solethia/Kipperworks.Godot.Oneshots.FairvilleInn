using FairvilleInn.Application.Ports;
using FairvilleInn.Domain;

namespace FairvilleInn.Application.UseCases;

public sealed class CloseDoorUseCase
{
    private readonly IPlayerMessenger _messenger;

    public CloseDoorUseCase(IPlayerMessenger messenger)
    {
        _messenger = messenger;
    }

    public DoorCloseResult Execute(Door door, bool obstructed)
    {
        ArgumentNullException.ThrowIfNull(door);

        var result = door.Close(obstructed);
        _messenger.Show(result switch
        {
            DoorCloseResult.Closed => $"You close the {door.Name}.",
            DoorCloseResult.AlreadyClosed => $"The {door.Name} is already closed.",
            DoorCloseResult.Obstructed => $"Something is in the way of the {door.Name}.",
            _ => $"Nothing happens to the {door.Name}.",
        });

        return result;
    }
}
