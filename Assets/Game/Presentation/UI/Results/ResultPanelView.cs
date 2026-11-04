using System.Collections.Generic;
using System.Text;
using NightCourier.Contracts;
using NightCourier.Economy;
using RD.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class ResultPanelView : MonoBehaviour
	{
		private const string SubtitleKey = "result.subtitle";
		private const string LineKey = "result.line";
		private const string TotalKey = "result.total";
		private const string RatingKey = "result.rating";
		private const string RankLineKey = "result.rank_line";
		private const string PromotedKey = "result.rank_promoted";
		private const string SaveFailedKey = "result.save_failed";
		private const string FailedFooterKey = "result.failed_footer";
		private const string StatGap = "     ";
		private const string AxisGap = "    ";
		private const int AxesPerLine = 4;
		private const char LineBreak = '\n';

		[SerializeField] private Text _subtitle;
		[SerializeField] private GameObject _successGroup;
		[SerializeField] private GameObject _failureGroup;
		[SerializeField] private Text _receiptCaption;
		[SerializeField] private Text _receiptLabels;
		[SerializeField] private Text _receiptValues;
		[SerializeField] private Text _totalLabel;
		[SerializeField] private Text _totalValue;
		[SerializeField] private Text _gradeCaption;
		[SerializeField] private Text _grade;
		[SerializeField] private Text _axes;
		[SerializeField] private Text _rank;
		[SerializeField] private Text _stats;
		[SerializeField] private Text _advice;
		[SerializeField] private Text _failureMessage;
		[SerializeField] private Text _failureFooter;

		public string Transcript { get; private set; } = string.Empty;

		public void ShowSuccess(
			ILocalizationService localization,
			ContractSpec spec,
			ContractSettlement settlement,
			ContractOutcome outcome,
			CourierRank rank,
			RankChange rankChange,
			bool saved)
		{
			StringBuilder transcript = new();

			_successGroup.SetActive(true);
			_failureGroup.SetActive(false);
			_subtitle.text = Subtitle(localization, spec);

			_receiptCaption.text = UiStyle.Caption(localization.Get("result.caption_settlement"));
			BuildReceipt(localization, settlement, transcript);

			_totalLabel.text = localization.Get("result.total_label");
			_totalValue.text = UiStyle.Tint(UiStyle.Money(settlement.Total), UiStyle.GoodColor);
			transcript.AppendLine(localization.Get(TotalKey, settlement.Total));

			ShiftRating rating = settlement.Rating;

			_gradeCaption.text = UiStyle.Caption(localization.Get("result.caption_rating"));
			_grade.text = UiStyle.Tint(rating.Overall.ToString(), UiStyle.GradeColor(rating.Overall));
			_axes.text = BuildAxes(localization, rating);
			transcript.AppendLine(localization.Get(RatingKey, rating.Overall));

			string rankTitle = localization.Get(rank.TitleKey);
			string rankText = localization.Get(RankLineKey, rankTitle, rankChange.ExperienceGained);

			if (rankChange.Promoted)
			{
				rankText += LineBreak + UiStyle.Tint(localization.Get(PromotedKey, rankTitle), UiStyle.GoodColor);
			}

			_rank.text = rankText;
			transcript.AppendLine(rankText);

			_stats.text = BuildStats(localization, outcome, transcript);
			_advice.text = BuildAdvice(localization, outcome, saved, transcript);

			Transcript = transcript.ToString().TrimEnd();
		}

		public void ShowFailure(ILocalizationService localization, ContractSpec spec, string reasonKey)
		{
			_successGroup.SetActive(false);
			_failureGroup.SetActive(true);
			_subtitle.text = Subtitle(localization, spec);

			string reason = localization.Get(reasonKey);
			string footer = localization.Get(FailedFooterKey);

			_failureMessage.text = reason;
			_failureFooter.text = UiStyle.Tint(footer, UiStyle.MutedColor);

			Transcript = reason + LineBreak + footer;
		}

		private static string Subtitle(ILocalizationService localization, ContractSpec spec)
		{
			return UiStyle.Tint(
				localization.Get(
					SubtitleKey,
					localization.Get(spec.TitleKey),
					localization.Get(ContractKeys.LocationKey(spec.DestinationSceneId))),
				UiStyle.MutedColor);
		}

		private void BuildReceipt(ILocalizationService localization, ContractSettlement settlement, StringBuilder transcript)
		{
			StringBuilder labels = new();
			StringBuilder values = new();

			foreach (SettlementLine line in settlement.Lines)
			{
				if (labels.Length > 0)
				{
					labels.Append(LineBreak);
					values.Append(LineBreak);
				}

				labels.Append(localization.Get(line.LabelKey));
				values.Append(FormatAmount(line));
				transcript.AppendLine(localization.Get(LineKey, localization.Get(line.LabelKey), line.Amount));
			}

			_receiptLabels.text = labels.ToString();
			_receiptValues.text = values.ToString();
		}

		private static string FormatAmount(SettlementLine line)
		{
			return line.Kind switch
			{
				SettlementLineKind.Bonus => UiStyle.Tint(UiStyle.SignedMoney(line.Amount), UiStyle.GoodColor),
				SettlementLineKind.Deduction => UiStyle.Tint(UiStyle.SignedMoney(line.Amount), UiStyle.DangerColor),
				_ => UiStyle.Money(line.Amount)
			};
		}

		private static string BuildAxes(ILocalizationService localization, ShiftRating rating)
		{
			List<string> axes = new(4);

			if (rating.DeliveryTime.HasValue)
			{
				axes.Add(Axis(localization, "result.axis.time", rating.DeliveryTime.Value));
			}

			axes.Add(Axis(localization, "result.axis.cargo", rating.CargoCondition));
			axes.Add(Axis(localization, "result.axis.protocol", rating.ProtocolCompliance));
			axes.Add(Axis(localization, "result.axis.property", rating.PropertyDamage));

			StringBuilder text = new();

			for (int index = 0; index < axes.Count; index++)
			{
				if (index > 0)
				{
					text.Append(index % AxesPerLine == 0 ? LineBreak.ToString() : AxisGap);
				}

				text.Append(axes[index]);
			}

			return text.ToString();
		}

		private static string Axis(ILocalizationService localization, string labelKey, ShiftGrade grade)
		{
			return UiStyle.Caption(localization.Get(labelKey)) + " " + UiStyle.Tint(grade.ToString(), UiStyle.GradeColor(grade));
		}

		private static string BuildStats(ILocalizationService localization, ContractOutcome outcome, StringBuilder transcript)
		{
			int integrityPercent = Mathf.RoundToInt((float)outcome.CargoIntegrity * 100f);

			string[] labels =
			{
				localization.Get("result.stat.violations"),
				localization.Get("result.stat.cargo"),
				localization.Get("result.stat.property"),
				localization.Get("result.stat.deaths")
			};

			string[] plain =
			{
				outcome.PackageViolationCount.ToString(),
				integrityPercent + "%",
				outcome.PropertyDamageCount.ToString(),
				outcome.DeathCount.ToString()
			};

			string[] colors =
			{
				outcome.PackageViolationCount == 0 ? UiStyle.GoodColor : UiStyle.WarningColor,
				UiStyle.IntegrityColor((float)outcome.CargoIntegrity),
				outcome.PropertyDamageCount == 0 ? UiStyle.GoodColor : UiStyle.WarningColor,
				outcome.DeathCount == 0 ? UiStyle.GoodColor : UiStyle.DangerColor
			};

			StringBuilder text = new();

			for (int index = 0; index < labels.Length; index++)
			{
				if (index > 0)
				{
					text.Append(StatGap);
				}

				text.Append(UiStyle.Caption(labels[index])).Append(' ').Append(UiStyle.Tint(plain[index], colors[index]));
				transcript.AppendLine(localization.Get(LineKey, labels[index], plain[index]));
			}

			return text.ToString();
		}

		private static string BuildAdvice(ILocalizationService localization, ContractOutcome outcome, bool saved, StringBuilder transcript)
		{
			StringBuilder text = new();

			text.Append(UiStyle.Heading(localization.Get("result.caption_debrief")));

			foreach (string key in SettlementAdvice.For(outcome))
			{
				string line = localization.Get(key);

				text.Append(LineBreak).Append(line);
				transcript.AppendLine(line);
			}

			if (!saved)
			{
				string warning = localization.Get(SaveFailedKey);

				text.Append(LineBreak).Append(UiStyle.Tint(warning, UiStyle.DangerColor));
				transcript.AppendLine(warning);
			}

			return text.ToString();
		}
	}
}
