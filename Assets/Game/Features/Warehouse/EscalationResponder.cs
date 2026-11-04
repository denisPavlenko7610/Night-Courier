using System;
using NightCourier.AI;
using NightCourier.Audio;
using NightCourier.Contracts;
using NightCourier.Player;
using RD.Audio;
using RD.Mechanics.ChaseTargetMemory;
using RD.Mechanics.Spawner;
using UnityEngine;

namespace NightCourier.UI
{
	public sealed class EscalationResponder : IDisposable
	{
		private const string UnstableNoticeKey = "notice.package_unstable";
		private const string HazardousNoticeKey = "notice.package_hazardous";
		private const string EnemySpawnedNoticeKey = "notice.enemy_spawned";
		private const string TooManyViolationsReasonKey = "result.too_many_violations";

		private readonly ContractRun _run;
		private readonly CargoPackage _package;
		private readonly PlayerSession _player;
		private readonly Spawner _alarmSpawner;
		private readonly PackageHudView _hud;
		private readonly WarehouseAudio _sounds;
		private readonly AudioService _audio;
		private readonly Action<string> _fail;

		public EscalationResponder(
			ContractRun run,
			CargoPackage package,
			PlayerSession player,
			Spawner alarmSpawner,
			PackageHudView hud,
			WarehouseAudio sounds,
			AudioService audio,
			Action<string> fail)
		{
			_run = run ?? throw new ArgumentNullException(nameof(run));
			_package = package != null ? package : throw new ArgumentNullException(nameof(package));
			_player = player != null ? player : throw new ArgumentNullException(nameof(player));
			_alarmSpawner = alarmSpawner != null ? alarmSpawner : throw new ArgumentNullException(nameof(alarmSpawner));
			_hud = hud != null ? hud : throw new ArgumentNullException(nameof(hud));
			_sounds = sounds != null ? sounds : throw new ArgumentNullException(nameof(sounds));
			_audio = audio ?? throw new ArgumentNullException(nameof(audio));
			_fail = fail ?? throw new ArgumentNullException(nameof(fail));

			_run.PackageViolationRegistered += OnViolationRegistered;
		}

		public void Dispose()
		{
			_run.PackageViolationRegistered -= OnViolationRegistered;
		}

		private void OnViolationRegistered(int count)
		{
			foreach (EscalationEffect effect in _run.Spec.GetEscalation(count))
			{
				switch (effect)
				{
					case EscalationEffect.PackageUnstable:
						_package.Unstable.Activate();
						_sounds.PlayUnstable();
						_hud.Notify(UnstableNoticeKey);
						break;
					case EscalationEffect.SpawnEnemy:
						SpawnAlarmEnemy();
						break;
					case EscalationEffect.PackageHazardous:
						_package.Hazard.Activate(_player.Contamination);
						_sounds.PlayHazard(_package.transform.position);
						_hud.Notify(HazardousNoticeKey);
						break;
					case EscalationEffect.FailContract:
						_fail(TooManyViolationsReasonKey);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(effect), effect, "Unknown escalation effect.");
				}
			}
		}

		private void SpawnAlarmEnemy()
		{
			if (!_alarmSpawner.TrySpawn(out GameObject enemy))
			{
				return;
			}

			enemy.GetComponent<EnemyAlert>().Bind(_player.transform);
			enemy.GetComponent<ChaseTargetMemory>().ReportTarget(_player.transform);
			enemy.GetComponent<EnemyAudio>().Bind(_audio);
			_sounds.PlayAlarm(enemy.transform.position);
			_hud.Notify(EnemySpawnedNoticeKey);
		}
	}
}
