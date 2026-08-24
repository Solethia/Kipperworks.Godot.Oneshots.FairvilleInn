using FairvilleInn.Application.UseCases;
using FairvilleInn.Infrastructure.Persistence;

namespace FairvilleInn.Presentation.Composition;

public static class GameComposition
{
	public static SaveGameUseCase CreateSaveGameUseCase(string savePath)
	{
		return new SaveGameUseCase(new JsonSaveGame(savePath));
	}
}
