namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;

	/// <summary>
	/// The recorded status of a phase of a node in an orchestration event.
	/// </summary>
	public sealed class OrchestrationEventPhase
	{
		internal OrchestrationEventPhase(string nodeId, OrchestrationPhaseStatus status)
		{
			if (status == null)
			{
				throw new ArgumentNullException(nameof(status));
			}

			NodeId = nodeId;
			Key = status.Key;
			Order = status.Order;
			State = status.State;
			Message = status.Message;
			LastUpdated = status.LastUpdated;
			Started = status.Started;
			Ended = status.Ended;
		}

		/// <summary>
		/// Gets the ID of the node the phase belongs to.
		/// </summary>
		public string NodeId { get; }

		/// <summary>
		/// Gets the key of the phase, unique within the orchestration script of the node.
		/// </summary>
		public string Key { get; }

		/// <summary>
		/// Gets the position of the phase within the orchestration script of the node.
		/// </summary>
		public int Order { get; }

		/// <summary>
		/// Gets the state of the phase.
		/// </summary>
		public PhaseState State { get; }

		/// <summary>
		/// Gets the message that explains the state, or <see langword="null"/> when there is none.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Gets the reason why the phase failed, or <see langword="null"/> when it did not fail.
		/// </summary>
		public string FailureReason => State == PhaseState.Failed ? Message : null;

		/// <summary>
		/// Gets the moment the status was last updated.
		/// </summary>
		public DateTimeOffset? LastUpdated { get; }

		/// <summary>
		/// Gets the moment the phase was last executed, or <see langword="null"/> when it was never executed.
		/// </summary>
		public DateTimeOffset? Started { get; }

		/// <summary>
		/// Gets the moment the phase completed or failed, or <see langword="null"/> when it did not end yet.
		/// </summary>
		public DateTimeOffset? Ended { get; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return Message == null ? $"{NodeId}/{Key}: {State}" : $"{NodeId}/{Key}: {State} ({Message})";
		}
	}
}
