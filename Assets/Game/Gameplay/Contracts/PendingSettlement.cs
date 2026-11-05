using System;
using RD.Save;

namespace NightCourier.Contracts
{
	[Serializable]
	public sealed class PendingSettlement
	{
		public string ContractId;
		public int CompletionCount;
		public long Total;
		public int DeliveryGrade;
		public ShiftGrade CargoGrade;
		public ShiftGrade ProtocolGrade;
		public ShiftGrade PropertyGrade;
		public long RankBefore;

		public ShiftRating Rating => new(DeliveryGrade < 0 ? null : (ShiftGrade)DeliveryGrade, CargoGrade, ProtocolGrade, PropertyGrade);

		public PendingSettlement Copy()
		{
			return (PendingSettlement)MemberwiseClone();
		}

		public void Validate()
		{
			if (string.IsNullOrEmpty(ContractId) || CompletionCount <= 0 || Total < 0 || RankBefore < 0
				|| DeliveryGrade < -1 || DeliveryGrade > (int)ShiftGrade.S
				|| !Enum.IsDefined(typeof(ShiftGrade), CargoGrade) || !Enum.IsDefined(typeof(ShiftGrade), ProtocolGrade)
				|| !Enum.IsDefined(typeof(ShiftGrade), PropertyGrade))
			{
				throw new InvalidSaveDataException("The pending settlement is invalid.");
			}
		}
	}
}
