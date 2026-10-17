using UnityEngine;

namespace NightCourier.Packages
{
	public interface ILightField
	{
		bool IsLit(Vector3 point);
	}
}
