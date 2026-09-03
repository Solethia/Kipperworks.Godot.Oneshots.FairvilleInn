namespace FairvilleInn.Domain;

public enum DoorOpenResult
{
    Opened,
    AlreadyOpen,
    Locked,
}

public enum DoorCloseResult
{
    Closed,
    AlreadyClosed,
    Obstructed,
}

public sealed class Door
{
    public Door(string name, bool isLocked = false)
    {
        Name = name;
        IsLocked = isLocked;
    }

    public string Name { get; }

    public bool IsOpen { get; private set; }

    public bool IsLocked { get; private set; }

    public DoorOpenResult Open()
    {
        if (IsOpen)
        {
            return DoorOpenResult.AlreadyOpen;
        }

        if (IsLocked)
        {
            return DoorOpenResult.Locked;
        }

        IsOpen = true;
        return DoorOpenResult.Opened;
    }

    // `obstructed` is reported by the caller (e.g. someone standing in the doorway).
    public DoorCloseResult Close(bool obstructed = false)
    {
        if (!IsOpen)
        {
            return DoorCloseResult.AlreadyClosed;
        }

        if (obstructed)
        {
            return DoorCloseResult.Obstructed;
        }

        IsOpen = false;
        return DoorCloseResult.Closed;
    }

    public void Unlock()
    {
        IsLocked = false;
    }
}
