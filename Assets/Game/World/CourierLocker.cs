using System;
using NightCourier.Player;
using RD.Mechanics.Checkpoint;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class CourierLocker : MonoBehaviour
	{
		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		[SerializeField] private Interactable _interactable;
		[SerializeField] private Checkpoint _checkpoint;
		[SerializeField] private Renderer _indicator;
		[SerializeField] private Color _idleColor = new(1f, 0.65f, 0.1f);
		[SerializeField] private Color _usedColor = new(0.1f, 0.35f, 0.15f);
		[SerializeField] private Color _activeColor = new(0.2f, 0.95f, 0.35f);

		private CheckpointSet _set;
		private MaterialPropertyBlock _block;
		private bool _used;

		public Checkpoint Checkpoint => _checkpoint;

		public void Bind(CheckpointSet set)
		{
			Unbind();

			_set = set != null ? set : throw new ArgumentNullException(nameof(set));
			_block ??= new MaterialPropertyBlock();
			_used = _checkpoint.IsActive;

			_interactable.Interacted += OnInteracted;
			_set.Activated += OnSetActivated;

			Refresh();
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_interactable != null)
			{
				_interactable.Interacted -= OnInteracted;
			}

			if (_set != null)
			{
				_set.Activated -= OnSetActivated;
				_set = null;
			}
		}

		private void OnInteracted()
		{
			_checkpoint.Activate();
		}

		private void OnSetActivated(Checkpoint activated)
		{
			if (activated == _checkpoint)
			{
				_used = true;
			}

			Refresh();
		}

		private void Refresh()
		{
			Color color = _checkpoint.IsActive ? _activeColor : _used ? _usedColor : _idleColor;

			_interactable.SetAvailable(!_used);
			_block.SetColor(BaseColorId, color);
			_indicator.SetPropertyBlock(_block);
		}
	}
}
