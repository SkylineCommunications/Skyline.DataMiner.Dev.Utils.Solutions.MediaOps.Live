namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;

	/// <summary>
	/// The recorded status of one phase of a node, for one event.
	/// </summary>
	internal class OrchestrationPhaseStatus
	{
		internal const int MaxMessageLength = 255;

		public OrchestrationPhaseStatus()
		{
			DomSection = new PhaseStatusSection();
			Order = 0;
			State = PhaseState.NotStarted;
		}

		internal OrchestrationPhaseStatus(PhaseStatusSection domSection)
		{
			DomSection = domSection ?? throw new ArgumentNullException(nameof(domSection));
		}

		internal PhaseStatusSection DomSection { get; }

		public string Key
		{
			get
			{
				return DomSection.PhaseKey;
			}

			set
			{
				DomSection.PhaseKey = value;
			}
		}

		public int Order
		{
			get
			{
				return (int)(DomSection.Order ?? 0);
			}

			set
			{
				DomSection.Order = value;
			}
		}

		public PhaseState State
		{
			get
			{
				return DomSection.State.HasValue ? (PhaseState)(int)DomSection.State.Value : PhaseState.NotStarted;
			}

			set
			{
				DomSection.State = (SlcOrchestrationIds.Enums.PhaseState)(int)value;
			}
		}

		/// <summary>
		/// Gets or sets the status message. Longer messages are trimmed to <see cref="MaxMessageLength"/> characters.
		/// </summary>
		public string Message
		{
			get
			{
				return DomSection.Message;
			}

			set
			{
				DomSection.Message = String.IsNullOrEmpty(value) ? null : value.Length > MaxMessageLength ? value.Substring(0, MaxMessageLength) : value;
			}
		}

		public DateTimeOffset? LastUpdated
		{
			get
			{
				return ToUtc(DomSection.LastUpdated);
			}

			set
			{
				DomSection.LastUpdated = value?.UtcDateTime;
			}
		}

		public DateTimeOffset? Started
		{
			get
			{
				return ToUtc(DomSection.Started);
			}

			set
			{
				DomSection.Started = value?.UtcDateTime;
			}
		}

		public DateTimeOffset? Ended
		{
			get
			{
				return ToUtc(DomSection.Ended);
			}

			set
			{
				DomSection.Ended = value?.UtcDateTime;
			}
		}

		private static DateTimeOffset? ToUtc(DateTime? value)
		{
			return value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
		}
	}
}
