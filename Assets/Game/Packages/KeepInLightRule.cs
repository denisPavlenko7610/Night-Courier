using System;
using NightCourier.Contracts;
using RD.Timers;
using UnityEngine;

namespace NightCourier.Packages
{
	public sealed class KeepInLightRule : PackageRule
	{
		[SerializeField, Min(1f)] private float _darkSeconds = 10f;

		private ILightField _lights;
		private CountdownTimer _timer;

		public override PackageRuleKind Kind => PackageRuleKind.KeepInLight;

		public double DarkSeconds => _timer?.ElapsedSeconds ?? 0d;

		public void Advance(double deltaSeconds, bool lit)
		{
			if (_timer == null)
			{
				throw new InvalidOperationException("The rule must be activated before it can advance.");
			}

			if (lit)
			{
				if (_timer.State != CountdownTimerState.Idle)
				{
					_timer.Reset();
				}

				return;
			}

			if (_timer.State == CountdownTimerState.Idle)
			{
				_timer.Restart();
			}

			_timer.Tick(deltaSeconds);
		}

		protected override void OnActivated(PackageRuleContext context)
		{
			_lights = context.Lights ?? throw new ArgumentException("A light field is required.", nameof(context));

			if (_timer != null)
			{
				_timer.Completed -= OnDarkLimitReached;
			}

			_timer = new CountdownTimer(_darkSeconds);
			_timer.Completed += OnDarkLimitReached;
		}

		private void Update()
		{
			Advance(Time.deltaTime, _lights.IsLit(transform.position));
		}

		private void OnDarkLimitReached()
		{
			_timer.Reset();
			Violate();
		}
	}
}
