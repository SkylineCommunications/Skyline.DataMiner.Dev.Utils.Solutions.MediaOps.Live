namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Scheduling;

	/// <summary>
	/// Persists the phase statuses of one node for one event on its <see cref="OrchestrationNodePhases"/> DOM instance.
	/// </summary>
	internal sealed class NodePhasesStatusStore : IPhaseStatusStore
	{
		private readonly NodePhasesRepository _repository;
		private readonly OrchestrationNodePhases _nodePhases;
		private readonly List<string> _declaredKeys;
		private readonly string _layoutFingerprint;
		private readonly string _storedLayoutFingerprint;
		private readonly Func<DateTimeOffset> _now;

		internal NodePhasesStatusStore(
			NodePhasesRepository repository,
			Guid jobInfoId,
			Guid eventId,
			string nodeId,
			IEnumerable<OrchestrationPhase> phases,
			Func<DateTimeOffset> now = null)
		{
			_repository = repository ?? throw new ArgumentNullException(nameof(repository));

			List<OrchestrationPhase> phaseList = phases?.ToList() ?? throw new ArgumentNullException(nameof(phases));
			_declaredKeys = phaseList.OfType<ExecutionPhase>().Select(x => x.Key).ToList();
			_layoutFingerprint = GetLayoutFingerprint(phaseList);
			_now = now ?? (() => DateTimeOffset.UtcNow);

			_nodePhases = repository.GetOrCreateNodePhases(jobInfoId, eventId, nodeId);
			_storedLayoutFingerprint = _nodePhases.PhaseLayoutFingerprint;
		}

		internal OrchestrationNodePhases NodePhases => _nodePhases;

		/// <summary>
		/// Gets the task of the resume that is still scheduled, if any.
		/// </summary>
		internal ScheduledTaskId PendingResumeTask => _nodePhases.ResumeSchedulerReference;

		/// <summary>
		/// Gets the wait phase to continue at, or <see langword="null"/> when there is none or the phases of the script changed since it was stored.
		/// </summary>
		internal ResumePoint GetResumePoint()
		{
			if (!String.Equals(_storedLayoutFingerprint, _layoutFingerprint, StringComparison.Ordinal)
				|| _nodePhases.ResumePhaseIndex == null
				|| _nodePhases.WaitStarted == null)
			{
				return null;
			}

			return new ResumePoint(_nodePhases.ResumePhaseIndex.Value, _nodePhases.WaitStarted.Value);
		}

		internal void SaveSuspension(ResumePoint suspendedAt, DateTimeOffset resumeTime, ScheduledTaskId resumeTask)
		{
			_nodePhases.ResumePhaseIndex = suspendedAt.PhaseIndex;
			_nodePhases.WaitStarted = suspendedAt.WaitStarted;
			_nodePhases.ResumeTime = resumeTime;
			_nodePhases.ResumeSchedulerReference = resumeTask;

			ApplyLayout();
			_repository.CreateOrUpdate(_nodePhases);
		}

		internal void ClearSuspension()
		{
			if (_nodePhases.ResumePhaseIndex == null
				&& _nodePhases.WaitStarted == null
				&& _nodePhases.ResumeTime == null
				&& _nodePhases.ResumeSchedulerReference == null)
			{
				return;
			}

			_nodePhases.ResumePhaseIndex = null;
			_nodePhases.WaitStarted = null;
			_nodePhases.ResumeTime = null;
			_nodePhases.ResumeSchedulerReference = null;

			_repository.CreateOrUpdate(_nodePhases);
		}

		public IReadOnlyDictionary<string, PhaseStatus> Load()
		{
			// Stored statuses of phases that are no longer declared are ignored, and removed on the next write.
			return _nodePhases.Phases
				.Where(x => _declaredKeys.Contains(x.Key, StringComparer.Ordinal))
				.ToDictionary(x => x.Key, ToPhaseStatus, StringComparer.Ordinal);
		}

		public void Record(string key, PhaseStatus status, DateTimeOffset? executionStarted)
		{
			OrchestrationPhaseStatus phase = _nodePhases.Phases.FirstOrDefault(x => String.Equals(x.Key, key, StringComparison.Ordinal));
			bool isNew = phase == null;

			if (isNew)
			{
				phase = new OrchestrationPhaseStatus { Key = key };
				_nodePhases.Phases.Add(phase);
			}

			PhaseState previousState = phase.State;
			string previousMessage = phase.Message;

			phase.State = status.State;
			phase.Message = status.Message;

			bool changed = isNew
				|| executionStarted.HasValue
				|| previousState != phase.State
				|| !String.Equals(previousMessage, phase.Message, StringComparison.Ordinal)
				|| !String.Equals(_nodePhases.PhaseLayoutFingerprint, _layoutFingerprint, StringComparison.Ordinal);

			if (!changed)
			{
				return;
			}

			DateTimeOffset now = _now();

			if (executionStarted.HasValue)
			{
				phase.Started = executionStarted;
			}

			if (IsFinal(phase.State))
			{
				if (executionStarted.HasValue || !IsFinal(previousState) || phase.Ended == null)
				{
					phase.Ended = now;
				}
			}
			else
			{
				phase.Ended = null;
			}

			phase.LastUpdated = now;

			ApplyLayout();
			_repository.CreateOrUpdate(_nodePhases);
		}

		internal static string GetLayoutFingerprint(IEnumerable<OrchestrationPhase> phases)
		{
			return String.Join(
				"|",
				phases.Select(x => x is WaitPhase wait
					? (wait.Node == null ? $"wait:{wait.PhaseKey}" : $"wait:{wait.Node}/{wait.PhaseKey}")
					: ((ExecutionPhase)x).Key));
		}

		private void ApplyLayout()
		{
			_nodePhases.PhaseLayoutFingerprint = _layoutFingerprint;

			foreach (OrchestrationPhaseStatus removed in _nodePhases.Phases.Where(x => !_declaredKeys.Contains(x.Key, StringComparer.Ordinal)).ToList())
			{
				_nodePhases.Phases.Remove(removed);
			}

			foreach (OrchestrationPhaseStatus phase in _nodePhases.Phases)
			{
				phase.Order = _declaredKeys.IndexOf(phase.Key);
			}
		}

		private static bool IsFinal(PhaseState state)
		{
			return state == PhaseState.Completed || state == PhaseState.Failed;
		}

		internal static PhaseStatus ToPhaseStatus(OrchestrationPhaseStatus phase)
		{
			switch (phase.State)
			{
				case PhaseState.InProgress:
					return PhaseStatus.InProgress(phase.Message);
				case PhaseState.Completed:
					return PhaseStatus.Completed(phase.Message);
				case PhaseState.Failed:
					return PhaseStatus.Failed(phase.Message);
				default:
					return PhaseStatus.NotStarted(phase.Message);
			}
		}
	}
}
