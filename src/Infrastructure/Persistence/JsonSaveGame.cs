using System.Text.Json;
using FairvilleInn.Application.Ports;
using FairvilleInn.Domain;

namespace FairvilleInn.Infrastructure.Persistence;

public sealed class JsonSaveGame : ISaveGame
{
    private readonly string _path;

    public JsonSaveGame(string path)
    {
        _path = path;
    }

    public void Save(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var json = JsonSerializer.Serialize(state);
        File.WriteAllText(_path, json);
    }
}
