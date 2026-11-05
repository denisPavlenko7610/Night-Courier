using System.Linq;
using System.Text;
using NightCourier.Contracts;
using NightCourier.Packages;
using RD.Localization;

namespace NightCourier.UI
{
	public static class ContractBriefing
	{
		private const string ListSeparator = ", ";
		private const string FieldGap = "     ";
		private const string LabelGap = "  ";
		private const char LineBreak = '\n';

		public static string SlotLabel(ContractSpec spec, ILocalizationService localization, bool selected)
		{
			string title = localization.Get(spec.TitleKey);
			string summary = localization.Get(
				"contract.slot_summary",
				UiStyle.Tint(localization.Get(ContractKeys.RiskKey(spec.Risk)), UiStyle.RiskColor(spec.Risk)),
				spec.BaseReward);

			return (selected ? UiStyle.Tint(title, UiStyle.AccentColor) : title) + LineBreak + summary;
		}

		public static string Story(ContractSpec spec, ILocalizationService localization)
		{
			StringBuilder text = new();

			text.Append(UiStyle.Tint(localization.Get(ContractKeys.OrderKey(spec.TitleKey)).ToUpperInvariant(), UiStyle.DocumentMuted)).Append(LineBreak);
			text.Append(localization.Get(spec.DescriptionKey));

			text.Append(LineBreak).Append(LineBreak);
			text.Append(UiStyle.Tint(localization.Get("contract.field.notes").ToUpperInvariant(), UiStyle.DocumentAccent));

			foreach (string note in localization.Get(ContractKeys.NotesKey(spec.TitleKey)).Split(LineBreak))
			{
				text.Append(LineBreak).Append(UiStyle.Tint(UiStyle.BulletSymbol, UiStyle.DocumentAccent)).Append(' ').Append(note);
			}

			text.Append(LineBreak).Append(LineBreak);
			text.Append(UiStyle.Tint(UiStyle.Italic(localization.Get(ContractKeys.MemoKey(spec.TitleKey))), UiStyle.DocumentMuted));

			return text.ToString();
		}

		public static string Terms(ContractSpec spec, ILocalizationService localization)
		{
			StringBuilder text = new();

			text.Append(Field(
				localization,
				"contract.field.risk",
				UiStyle.Tint(localization.Get(ContractKeys.RiskKey(spec.Risk)), UiStyle.RiskColor(spec.Risk))));
			text.Append(FieldGap).Append(Field(localization, "contract.field.pay", UiStyle.Money(spec.BaseReward)));

			text.Append(LineBreak).Append(Field(
				localization,
				"contract.field.site",
				localization.Get(
					"contract.site",
					localization.Get(ContractKeys.LocationKey(spec.DestinationSceneId)),
					localization.Get(ContractKeys.StateKey(spec.LocationState)))));

			text.Append(LineBreak).Append(Field(
				localization,
				"contract.field.rules",
				spec.PackageRules.Count == 0
					? localization.Get("contract.none")
					: string.Join(ListSeparator, spec.PackageRules.Select(rule => localization.Get(PackageRuleKeys.NameKey(rule))))));

			foreach (PackageRuleKind rule in spec.PackageRules)
			{
				text.Append(LineBreak).Append(UiStyle.Tint(localization.Get(PackageRuleKeys.BriefKey(rule)), UiStyle.MutedColor));
			}

			if (spec.HasTimeLimit)
			{
				text.Append(LineBreak).Append(Field(localization, "contract.field.deadline", ClockText.Format(spec.TimeLimitSeconds)));
			}

			foreach (ContractObjectiveSpec objective in spec.Objectives)
			{
				text.Append(LineBreak).Append(Field(
					localization,
					"contract.field.bonus",
					localization.Get(objective.LabelKey) + LabelGap + UiStyle.Tint(UiStyle.SignedMoney(objective.BonusCredits), UiStyle.GoodColor)));
			}

			if (spec.Encounters.Count > 0)
			{
				text.Append(LineBreak).Append(Field(
					localization,
					"contract.field.threats",
					string.Join(ListSeparator, spec.Encounters.Select(threat => localization.Get(ContractKeys.ThreatKey(threat))))));
			}

			return text.ToString();
		}

		private static string Field(ILocalizationService localization, string labelKey, string value)
		{
			return UiStyle.Caption(localization.Get(labelKey)) + LabelGap + value;
		}
	}
}
