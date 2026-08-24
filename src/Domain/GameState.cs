namespace FairvilleInn.Domain;

public sealed class GameState
{
    public int Day { get; private set; } = 1;

    public void AdvanceDay()
    {
        Day++;
    }
}
