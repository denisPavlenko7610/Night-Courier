using NightCourier.Contracts;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class EncounterGroup : MonoBehaviour
	{
		[SerializeField] private EncounterKind _kind;

		public EncounterKind Kind => _kind;
	}
}
