using System;
using System.Collections.Generic;
using System.Text;
using NightCourier.Contracts;
using NightCourier.Economy;
using RD.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class ResultPanelView : MonoBehaviour
	{
		public const string PanelName = "result-panel";
		public const string ReturnButtonName = "result-return";

		private const string TitleKey = "result.title";
		private const string FailedTitleKey = "result.failed_title";
		private const string PendingTitleKey = "result.pending_title";
		private const string CreditsKey = "depot.credits";
		private const string SubtitleKey = "result.subtitle";
		private const string LineKey = "result.line";
		private const string TotalKey = "result.total";
		private const string RatingKey = "result.rating";
		private const string RankLineKey = "result.rank_line";
		private const string PromotedKey = "result.rank_promoted";
		private const string SaveFailedKey = "result.save_failed";
		private const string FailedFooterKey = "result.failed_footer";
		private const string PendingFooterKey = "result.pending_footer";
		private const string TitleName = "result-title";
		private const string SubtitleName = "result-subtitle";
		private const string SuccessName = "result-success";
		private const string FailureName = "result-failure";
		private const string CreditsName = "result-credits";
		private const string ReceiptCaptionName = "receipt-caption";
		private const string ReceiptLabelsName = "receipt-labels";
		private const string ReceiptValuesName = "receipt-values";
		private const string TotalLabelName = "total-label";
		private const string TotalValueName = "total-value";
		private const string GradeCaptionName = "grade-caption";
		private const string GradeName = "grade";
		private const string AxesName = "axes";
		private const string RankName = "rank";
		private const string StatsName = "stats";
		private const string AdviceName = "advice";
		private const string FailureMessageName = "failure-message";
		private const string FailureFooterName = "failure-footer";
		private const string StatGap = "     ";
		private const string AxisGap = "    ";
		private const int AxesPerLine = 4;
		private const char LineBreak = '\n';

		[SerializeField] private UIDocument _document;

		private ILocalizationService _localization;
		private VisualElement _panel;
		private Label _title;
		private Label _subtitle;
		private VisualElement _successGroup;
		private VisualElement _failureGroup;
		private Label _credits;
		private Label _receiptCaption;
		private Label _receiptLabels;
		private Label _receiptValues;
		private Label _totalLabel;
		private Label _totalValue;
		private Label _gradeCaption;
		private Label _grade;
		private Label _axes;
		private Label _rank;
		private Label _stats;
		private Label _advice;
		private Label _failureMessage;
		private Label _failureFooter;
		private Button _return;

		public event Action ReturnRequested;

		public string Transcript { get; private set; } = string.Empty;

		public VisualElement Panel => _panel;

		public bool IsOpen => _panel != null && _panel.style.display != DisplayStyle.None;

		public bool ReturnEnabled => _return != null && _return.enabledSelf;

		public string TitleText => _title?.text;

		public void Bind(ILocalizationService localization)
		{
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));

			VisualElement root = _document.rootVisualElement;

			_panel = UiQuery.Require<VisualElement>(root, PanelName);
			_title = UiQuery.Require<Label>(_panel, TitleName);
			_subtitle = UiQuery.Require<Label>(_panel, SubtitleName);
			_successGroup = UiQuery.Require<VisualElement>(_panel, SuccessName);
			_failureGroup = UiQuery.Require<VisualElement>(_panel, FailureName);
			_credits = UiQuery.Require<Label>(_panel, CreditsName);
			_receiptCaption = UiQuery.Require<Label>(_panel, ReceiptCaptionName);
			_receiptLabels = UiQuery.Require<Label>(_panel, ReceiptLabelsName);
			_receiptValues = UiQuery.Require<Label>(_panel, ReceiptValuesName);
			_totalLabel = UiQuery.Require<Label>(_panel, TotalLabelName);
			_totalValue = UiQuery.Require<Label>(_panel, TotalValueName);
			_gradeCaption = UiQuery.Require<Label>(_panel, GradeCaptionName);
			_grade = UiQuery.Require<Label>(_panel, GradeName);
			_axes = UiQuery.Require<Label>(_panel, AxesName);
			_rank = UiQuery.Require<Label>(_panel, RankName);
			_stats = UiQuery.Require<Label>(_panel, StatsName);
			_advice = UiQuery.Require<Label>(_panel, AdviceName);
			_failureMessage = UiQuery.Require<Label>(_panel, FailureMessageName);
			_failureFooter = UiQuery.Require<Label>(_panel, FailureFooterName);
			_return = UiQuery.Require<Button>(_panel, ReturnButtonName);

			LocalizedText.Apply(_panel, localization);

			_return.clicked += OnReturnClicked;

			UiQuery.SetVisible(_panel, false);
		}

		public void Open(long credits)
		{
			_credits.text = _localization.Get(CreditsKey, credits);

			UiQuery.SetVisible(_panel, true);
			_panel.schedule.Execute(FocusReturn);
		}

		public void SetReturnEnabled(bool enabled)
		{
			_return.SetEnabled(enabled);
		}

		public void ShowSuccess(
			ILocalizationService localization,
			ContractSpec spec,
			ContractSettlement settlement,
			ContractOutcome outcome,
			CourierRank rank,
			RankChange rankChange,
			bool saved
		)
		{
			StringBuilder transcript = new();

			UiQuery.SetVisible(_successGroup, true);
			UiQuery.SetVisible(_failureGroup, false);
			_title.text = localization.Get(TitleKey);
			_subtitle.text = Subtitle(localization, spec);

			_receiptCaption.text = UiStyle.Tint(
				localization.Get("result.caption_settlement").ToUpperInvariant(),
				UiStyle.DocumentMuted
			);
			BuildReceipt(localization, settlement, transcript);

			_totalLabel.text = localization.Get("result.total_label");
			_totalValue.text = UiStyle.Tint(UiStyle.Money(settlement.Total), UiStyle.DocumentGood);
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

		public void ShowFailure(
			ILocalizationService localization,
			ContractSpec spec,
			string reasonKey,
			bool paymentPending = false
		)
		{
			UiQuery.SetVisible(_successGroup, false);
			UiQuery.SetVisible(_failureGroup, true);
			_title.text = localization.Get(paymentPending ? PendingTitleKey : FailedTitleKey);
			_subtitle.text = Subtitle(localization, spec);

			string reason = localization.Get(reasonKey);
			string footer = localization.Get(paymentPending ? PendingFooterKey : FailedFooterKey);

			_failureMessage.text = reason;
			_failureFooter.text = UiStyle.Tint(footer, UiStyle.MutedColor);

			Transcript = reason + LineBreak + footer;
		}

		private void OnDestroy()
		{
			if (_return != null)
			{
				_return.clicked -= OnReturnClicked;
			}
		}

		private void FocusReturn()
		{
			if (IsOpen && _return.enabledSelf)
			{
				_return.Focus();
			}
		}

		private void OnReturnClicked()
		{
			ReturnRequested?.Invoke();
		}

		private static string Subtitle(ILocalizationService localization, ContractSpec spec)
		{
			return UiStyle.Tint(
				localization.Get(
					SubtitleKey,
					localization.Get(spec.TitleKey),
					localization.Get(ContractKeys.LocationKey(spec.DestinationSceneId))
				),
				UiStyle.MutedColor
			);
		}

		private void BuildReceipt(
			ILocalizationService localization,
			ContractSettlement settlement,
			StringBuilder transcript
		)
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
				SettlementLineKind.Bonus => UiStyle.Tint(UiStyle.SignedMoney(line.Amount), UiStyle.DocumentGood),
				SettlementLineKind.Deduction => UiStyle.Tint(UiStyle.SignedMoney(line.Amount), UiStyle.DocumentDanger),
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
			return UiStyle.Caption(localization.Get(labelKey)) +
				" " +
				UiStyle.Tint(grade.ToString(), UiStyle.GradeColor(grade));
		}

		private static string BuildStats(
			ILocalizationService localization,
			ContractOutcome outcome,
			StringBuilder transcript
		)
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

				text.Append(UiStyle.Caption(labels[index])).Append(' ')
					.Append(UiStyle.Tint(plain[index], colors[index]));
				transcript.AppendLine(localization.Get(LineKey, labels[index], plain[index]));
			}

			return text.ToString();
		}

		private static string BuildAdvice(
			ILocalizationService localization,
			ContractOutcome outcome,
			bool saved,
			StringBuilder transcript
		)
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
