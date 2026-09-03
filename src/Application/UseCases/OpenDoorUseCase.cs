using FairvilleInn.Application.Ports;
using FairvilleInn.Domain;

namespace FairvilleInn.Application.UseCases;

public sealed class OpenDoorUseCase
{
    private readonly IPlayerMessenger _messenger;

    public OpenDoorUseCase(IPlayerMessenger messenger)
    {
        _messenger = messenger;
    }

    public DoorOpenResult Execute(Door door)
    {
        ArgumentNullException.ThrowIfNull(door);

        var result = door.Open();
        _messenger.Show(result switch
        {
            DoorOpenResult.Opened => $"You open the {door.Name}.",
            DoorOpenResult.AlreadyOpen => $"The {door.Name} is already open.",
            DoorOpenResult.Locked => $"The {door.Name} is locked.",
            _ => $"Nothing happens to the {door.Name}.",
        });

        return result;
    }
}
