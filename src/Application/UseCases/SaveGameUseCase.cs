using FairvilleInn.Application.Ports;
using FairvilleInn.Domain;

namespace FairvilleInn.Application.UseCases;

public sealed class SaveGameUseCase
{
	private readonly ISaveGame _saveGame;

	public SaveGameUseCase(ISaveGame saveGame)
	{
		_saveGame = saveGame;
	}

	public void Execute(GameState state)
	{
		ArgumentNullException.ThrowIfNull(state);
		_saveGame.Save(state);
	}
}
