using FairvilleInn.Domain;

namespace FairvilleInn.Application.Ports;

public interface ISaveGame
{
    void Save(GameState state);
}
