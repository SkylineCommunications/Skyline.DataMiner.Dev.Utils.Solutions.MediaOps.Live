namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;

	/// <summary>
	/// A phase that waits until another phase reports <see cref="PhaseState.Completed"/>.
	/// A wait phase is not shown as a phase of its own; what it waits for is reported on the phase defined after it.
	/// </summary>
	public sealed class WaitPhase : OrchestrationPhase
	{
		internal static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromSeconds(10);
		internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);

		internal WaitPhase(string phaseKey, string node = null)
		{
			if (String.IsNullOrWhiteSpace(phaseKey))
			{
				throw new ArgumentException("The key of the phase to wait for cannot be empty.", nameof(phaseKey));
			}

			if (node != null && String.IsNullOrWhiteSpace(node))
			{
				throw new ArgumentException("The node to wait for cannot be empty.", nameof(node));
			}

			PhaseKey = phaseKey;
			Node = node;
		}

		/// <summary>
		/// Gets the key of the phase that is waited for.
		/// </summary>
		public string PhaseKey { get; }

		/// <summary>
		/// Gets the ID or label of the node whose phase is waited for, or <see langword="null"/> for a phase of the same node.
		/// </summary>
		public string Node { get; }

		internal TimeSpan PollingInterval { get; private set; } = DefaultPollingInterval;

		internal TimeSpan TimeoutDuration { get; private set; } = DefaultTimeout;

		/// <summary>
		/// Sets the interval at which the status of the phase is polled. Defaults to 10 seconds.
		/// </summary>
		/// <param name="interval">The polling interval.</param>
		/// <returns>This phase.</returns>
		public WaitPhase PollEvery(TimeSpan interval)
		{
			if (interval <= TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException(nameof(interval), "The polling interval must be positive.");
			}

			PollingInterval = interval;
			return this;
		}

		/// <summary>
		/// Sets how long to wait before the phase defined after this wait is marked as failed. Defaults to 10 minutes.
		/// </summary>
		/// <param name="timeout">The timeout.</param>
		/// <returns>This phase.</returns>
		public WaitPhase Timeout(TimeSpan timeout)
		{
			if (timeout <= TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException(nameof(timeout), "The timeout must be positive.");
			}

			TimeoutDuration = timeout;
			return this;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return Node == null ? $"Wait for phase '{PhaseKey}'" : $"Wait for phase '{PhaseKey}' of node '{Node}'";
		}
	}
}
