using System.Threading;
using UnityEngine;

namespace NightCourier.App
{
	public interface IGameNavigator
	{
		bool IsNavigating { get; }

		Awaitable GoToAsync(string sceneId, CancellationToken cancellationToken = default);

		void Quit();
	}
}
