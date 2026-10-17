using RD.DependencyInjection;

namespace NightCourier.App
{
	public interface ISceneEntry
	{
		void Enter(GameServices services, IObjectResolver scene);
	}
}
