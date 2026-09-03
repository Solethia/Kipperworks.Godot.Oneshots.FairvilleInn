using FairvilleInn.Domain;

namespace FairvilleInn.Domain.Tests;

public sealed class DoorTests
{
    [Fact]
    public void Open_ClosedUnlockedDoor_Opens()
    {
        var door = new Door("front");

        var result = door.Open();

        Assert.Equal(DoorOpenResult.Opened, result);
        Assert.True(door.IsOpen);
    }

    [Fact]
    public void Open_LockedDoor_ReportsLocked()
    {
        var door = new Door("cellar", isLocked: true);

        var result = door.Open();

        Assert.Equal(DoorOpenResult.Locked, result);
        Assert.False(door.IsOpen);
    }

    [Fact]
    public void Close_ObstructedDoor_StaysOpen()
    {
        var door = new Door("front");
        door.Open();

        var result = door.Close(obstructed: true);

        Assert.Equal(DoorCloseResult.Obstructed, result);
        Assert.True(door.IsOpen);
    }
}
