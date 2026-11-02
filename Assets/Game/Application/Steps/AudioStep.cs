using System.Threading;
using RD.Audio;
using RD.Foundation;
using RD.Startup;
using UnityEngine;

namespace NightCourier.App
{
	public sealed class AudioStep : StartupStep
	{
		public const string StepId = "audio";

		private const int MaxVoices = 32;
		private const int PrewarmVoices = 8;
		private const float FollowFadeSeconds = 0.15f;

		private readonly GameServicesBuilder _builder;
		private readonly Transform _owner;

		private AudioService _audio;

		public AudioStep(GameServicesBuilder builder, Transform owner) : base(StepId)
		{
			_builder = builder;
			_owner = owner;
		}

		public override Awaitable StartAsync(CancellationToken cancellationToken = default)
		{
			_audio = new AudioService(
				_owner,
				new AudioServiceOptions(
					maxVoiceCount: MaxVoices,
					prewarmVoiceCount: PrewarmVoices,
					followTargetDestroyedBehavior: FollowTargetDestroyedBehavior.StopWithFade,
					followTargetDestroyedFadeSeconds: FollowFadeSeconds));
			_builder.Audio = _audio;

			return AwaitableExtensions.Completed;
		}

		public override Awaitable StopAsync(CancellationToken cancellationToken = default)
		{
			_audio?.Dispose();
			_audio = null;

			return AwaitableExtensions.Completed;
		}
	}
}
