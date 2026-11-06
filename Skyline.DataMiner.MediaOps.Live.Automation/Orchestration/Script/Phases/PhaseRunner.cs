namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;

	/// <summary>
	/// Walks the phases of an orchestration script in order and decides, per phase, whether it is skipped, executed or waited for.
	/// </summary>
	internal sealed class PhaseRunner
	{
		internal const int StatusAttempts = 3;

		/// <summary>
		/// A poll at least this far away ends the execution and is resumed by a scheduled task instead of holding the thread.
		/// A scheduled task must be at least 5 seconds in the future to trigger reliably.
		/// </summary>
		internal static readonly TimeSpan HoldThreshold = TimeSpan.FromSeconds(10);

		private readonly IReadOnlyList<OrchestrationPhase> _phases;
		private readonly OrchestrationPhaseContext _context;
		private readonly Action<TimeSpan> _sleep;
		private readonly Func<DateTimeOffset> _now;
		private readonly IPhaseStatusStore _store;
		private readonly bool _canSuspend;
		private readonly IEventNodes _eventNodes;
		private readonly Dictionary<string, PhaseStatus> _statuses = new Dictionary<string, PhaseStatus>(StringComparer.Ordinal);
		private readonly Dictionary<int, WaitTarget> _waitTargets = new Dictionary<int, WaitTarget>();

		internal PhaseRunner(
			IEnumerable<OrchestrationPhase> phases,
			OrchestrationPhaseContext context,
			IPhaseStatusStore store = null,
			Action<TimeSpan> sleep = null,
			Func<DateTimeOffset> now = null,
			bool canSuspend = false,
			IEventNodes eventNodes = null)
		{
			_phases = phases?.ToList() ?? throw new ArgumentNullException(nameof(phases));
			_context = context ?? throw new ArgumentNullException(nameof(context));
			_store = store ?? new InMemoryPhaseStatusStore();
			_sleep = sleep ?? (duration => Sleep(context.Engine, duration));
			_now = now ?? (() => DateTimeOffset.UtcNow);
			_canSuspend = canSuspend;
			_eventNodes = eventNodes;

			foreach (KeyValuePair<string, PhaseStatus> recorded in _store.Load())
			{
				_statuses[recorded.Key] = recorded.Value;
			}
		}

		/// <summary>
		/// Gets the last known status per phase key.
		/// </summary>
		internal IReadOnlyDictionary<string, PhaseStatus> Statuses => _statuses;

		/// <param name="resumePoint">The wait phase to continue at, or <see langword="null"/> to walk all phases from the first one.</param>
		internal PhaseRunResult Run(ResumePoint resumePoint = null)
		{
			if (!TryResolve(out string resolveFailure))
			{
				return PhaseRunResult.Failure(resolveFailure);
			}

			bool resumes = resumePoint != null
				&& resumePoint.PhaseIndex >= 0
				&& resumePoint.PhaseIndex < _phases.Count
				&& _phases[resumePoint.PhaseIndex] is WaitPhase;

			for (int i = resumes ? resumePoint.PhaseIndex : 0; i < _phases.Count; i++)
			{
				PhaseRunResult result = _phases[i] is WaitPhase wait
					? RunWait(wait, i, resumes && i == resumePoint.PhaseIndex ? resumePoint.WaitStarted : (DateTimeOffset?)null)
					: RunExecution((ExecutionPhase)_phases[i]);

				if (result != null)
				{
					return result;
				}
			}

			return PhaseRunResult.Success();
		}

		private bool TryResolve(out string failure)
		{
			if (_phases.Any(x => x == null))
			{
				failure = "The orchestration script defines a phase that is null.";
				return false;
			}

			string duplicateKey = _phases
				.OfType<ExecutionPhase>()
				.GroupBy(x => x.Key, StringComparer.Ordinal)
				.FirstOrDefault(g => g.Count() > 1)?.Key;

			if (duplicateKey != null)
			{
				failure = $"Phase '{duplicateKey}' is defined more than once.";
				return false;
			}

			foreach (ExecutionPhase phase in _phases.OfType<ExecutionPhase>())
			{
				if (!_statuses.ContainsKey(phase.Key))
				{
					_statuses[phase.Key] = PhaseStatus.NotStarted();
				}
			}

			for (int i = 0; i < _phases.Count; i++)
			{
				if (!(_phases[i] is WaitPhase wait))
				{
					continue;
				}

				if (!TryResolveWait(wait, i, out WaitTarget target, out string reason))
				{
					ExecutionPhase next = FindNextExecutionPhase(i);
					if (next != null)
					{
						Record(next.Key, PhaseStatus.Failed(reason));
					}

					failure = reason;
					return false;
				}

				_waitTargets[i] = target;
			}

			failure = null;
			return true;
		}

		private bool TryResolveWait(WaitPhase wait, int index, out WaitTarget target, out string failure)
		{
			target = null;

			if (wait.Node != null)
			{
				string unresolved = $"Unable to resolve the '{wait.PhaseKey}' phase of node '{wait.Node}'";

				if (_eventNodes == null)
				{
					failure = $"{unresolved}: waiting on another node is only possible when the script runs for a node of an event.";
					return false;
				}

				if (!_eventNodes.TryResolveNode(wait.Node, out EventNode node, out string nodeFailure))
				{
					failure = $"{unresolved}: {nodeFailure}.";
					return false;
				}

				// The phase of another node cannot be validated: that node may not have run yet.
				if (!node.IsCurrentNode)
				{
					target = WaitTarget.ForOtherNode(node, wait.PhaseKey);
					failure = null;
					return true;
				}
			}

			ExecutionPhase phase = FindEarlierPhase(wait.PhaseKey, index);
			if (phase == null)
			{
				failure = $"Unable to resolve the '{wait.PhaseKey}' phase: it is not defined before the wait.";
				return false;
			}

			if (phase.StatusMethod == null)
			{
				failure = $"Unable to resolve the '{wait.PhaseKey}' phase: it has no status method to wait on.";
				return false;
			}

			target = WaitTarget.ForCurrentNode(phase);
			failure = null;
			return true;
		}

		private PhaseRunResult RunExecution(ExecutionPhase phase)
		{
			PhaseStatus status;

			if (phase.StatusMethod != null)
			{
				if (!TryGetStatus(phase, out status, out string statusFailure))
				{
					Record(phase.Key, PhaseStatus.Failed(statusFailure));
					return PhaseRunResult.Failure(statusFailure);
				}
			}
			else
			{
				// Without a status method, only a recorded completion skips the phase.
				status = _statuses[phase.Key].State == PhaseState.Completed ? _statuses[phase.Key] : PhaseStatus.NotStarted();
			}

			DateTimeOffset? executionStarted = null;
			if (status.State == PhaseState.NotStarted)
			{
				executionStarted = _now();
				status = Execute(phase);
			}

			Record(phase.Key, status, executionStarted);

			return status.State == PhaseState.Failed ? PhaseRunResult.Failure(FormatFailure($"phase '{phase.Key}'", status)) : null;
		}

		private PhaseRunResult RunWait(WaitPhase wait, int index, DateTimeOffset? waitStarted)
		{
			WaitTarget target = _waitTargets[index];
			ExecutionPhase next = FindNextExecutionPhase(index);
			DateTimeOffset started = waitStarted ?? _now();
			bool reported = false;

			while (true)
			{
				if (!TryPoll(target, out PhaseStatus status, out string pollFailure))
				{
					// A phase of another node is never written here; its failure is reported on the phase after the wait.
					Record(target.Phase?.Key ?? next?.Key, PhaseStatus.Failed(pollFailure));
					return PhaseRunResult.Failure(pollFailure);
				}

				if (target.Phase != null)
				{
					Record(target.Phase.Key, status);
				}

				reported |= status != null;
				status = status ?? PhaseStatus.NotStarted();

				if (status.State == PhaseState.Completed)
				{
					return null;
				}

				if (status.State == PhaseState.Failed)
				{
					string failed = FormatFailure(target.Description, status);
					if (target.Phase == null)
					{
						Record(next?.Key, PhaseStatus.Failed(failed));
					}

					return PhaseRunResult.Failure(failed);
				}

				TimeSpan elapsed = _now() - started;
				if (elapsed >= wait.TimeoutDuration)
				{
					string outcome = reported ? "did not complete" : "never reported a status";
					string timeout = $"{Capitalize(target.Description)} {outcome} within '{FormatDuration(wait.TimeoutDuration)}'.";
					Record(next?.Key, PhaseStatus.Failed(timeout));

					return PhaseRunResult.Failure(timeout);
				}

				Record(next?.Key, PhaseStatus.NotStarted($"Waiting on {target.Description}."));

				TimeSpan remaining = wait.TimeoutDuration - elapsed;
				TimeSpan untilNextPoll = remaining < wait.PollingInterval ? remaining : wait.PollingInterval;

				if (_canSuspend && untilNextPoll >= HoldThreshold)
				{
					return PhaseRunResult.Suspended(new ResumePoint(index, started), _now() + untilNextPoll, next?.Key);
				}

				try
				{
					_sleep(untilNextPoll);
				}
				catch (ScriptTimeoutException)
				{
					RecordScriptTimeout(next?.Key, $"waiting on {target.Description}");
					throw;
				}
			}
		}

		private PhaseStatus Execute(ExecutionPhase phase)
		{
			try
			{
				return phase.ExecuteMethod(_context) ?? PhaseStatus.Failed($"Phase '{phase.Key}' did not return a status.");
			}
			catch (ScriptTimeoutException)
			{
				RecordScriptTimeout(phase.Key, $"executing phase '{phase.Key}'");
				throw;
			}
			catch (Exception e) when (!IsAbort(e))
			{
				return PhaseStatus.Failed(e.Message);
			}
		}

		private bool TryGetStatus(ExecutionPhase phase, out PhaseStatus status, out string failure)
		{
			string lastError = null;

			// A transient problem in the status call must not cause the phase to be executed again.
			for (int attempt = 0; attempt < StatusAttempts; attempt++)
			{
				try
				{
					status = phase.StatusMethod(_context);
					if (status != null)
					{
						failure = null;
						return true;
					}

					lastError = "the status method returned no status";
				}
				catch (ScriptTimeoutException)
				{
					RecordScriptTimeout(phase.Key, $"retrieving the status of phase '{phase.Key}'");
					throw;
				}
				catch (Exception e) when (!IsAbort(e))
				{
					lastError = e.Message;
				}
			}

			status = null;
			failure = $"Unable to retrieve status of '{phase.Key}' phase: {lastError}";
			return false;
		}

		/// <summary>
		/// Gets the status of the phase that is waited for. For a phase of another node, <paramref name="status"/> is
		/// <see langword="null"/> when that node did not store a status for it yet.
		/// </summary>
		private bool TryPoll(WaitTarget target, out PhaseStatus status, out string failure)
		{
			if (target.Phase != null)
			{
				return TryGetStatus(target.Phase, out status, out failure);
			}

			string lastError = null;

			for (int attempt = 0; attempt < StatusAttempts; attempt++)
			{
				try
				{
					status = _eventNodes.GetStoredStatus(target.Node.NodeId, target.PhaseKey);
					failure = null;
					return true;
				}
				catch (Exception e) when (!IsAbort(e))
				{
					lastError = e.Message;
				}
			}

			status = null;
			failure = $"Unable to retrieve status of '{target.PhaseKey}' phase of node '{target.Node.DisplayName}': {lastError}";
			return false;
		}

		private void Record(string key, PhaseStatus status, DateTimeOffset? executionStarted = null)
		{
			if (key == null)
			{
				return;
			}

			_statuses[key] = status;
			_store.Record(key, status, executionStarted);
		}

		private void RecordScriptTimeout(string key, string activity)
		{
			try
			{
				Record(key, PhaseStatus.Failed($"The script timed out while {activity}."));
			}
			catch (Exception e) when (!IsAbort(e))
			{
				// Best effort: the script timeout must still end the script.
			}
		}

		private ExecutionPhase FindEarlierPhase(string key, int beforeIndex)
		{
			return _phases
				.Take(beforeIndex)
				.OfType<ExecutionPhase>()
				.FirstOrDefault(x => String.Equals(x.Key, key, StringComparison.Ordinal));
		}

		private ExecutionPhase FindNextExecutionPhase(int afterIndex)
		{
			return _phases
				.Skip(afterIndex + 1)
				.OfType<ExecutionPhase>()
				.FirstOrDefault();
		}

		private static string FormatFailure(string description, PhaseStatus status)
		{
			return status.Message == null ? $"{Capitalize(description)} failed." : $"{Capitalize(description)} failed: {status.Message}";
		}

		private static string Capitalize(string text)
		{
			return String.IsNullOrEmpty(text) ? text : Char.ToUpperInvariant(text[0]) + text.Substring(1);
		}

		private static string FormatDuration(TimeSpan duration)
		{
			if (duration.TotalHours >= 1 && duration.Ticks % TimeSpan.TicksPerHour == 0)
			{
				return Pluralize((long)duration.TotalHours, "hour");
			}

			if (duration.TotalMinutes >= 1 && duration.Ticks % TimeSpan.TicksPerMinute == 0)
			{
				return Pluralize((long)duration.TotalMinutes, "minute");
			}

			if (duration.Ticks % TimeSpan.TicksPerSecond == 0)
			{
				return Pluralize((long)duration.TotalSeconds, "second");
			}

			return duration.ToString();
		}

		private static string Pluralize(long count, string unit)
		{
			return count == 1 ? $"1 {unit}" : $"{count} {unit}s";
		}

		internal static bool IsAbort(Exception e)
		{
			return e is ScriptAbortException || e is ScriptForceAbortException || e is ScriptTimeoutException;
		}

		private static void Sleep(IEngine engine, TimeSpan duration)
		{
			if (engine != null)
			{
				engine.Sleep((int)duration.TotalMilliseconds);
			}
			else
			{
				Thread.Sleep(duration);
			}
		}

		/// <summary>
		/// The phase a wait polls: a phase of the current node, or the stored status of a phase of another node.
		/// </summary>
		private sealed class WaitTarget
		{
			private WaitTarget(ExecutionPhase phase, EventNode node, string phaseKey)
			{
				Phase = phase;
				Node = node;
				PhaseKey = phaseKey;
			}

			// Null for a phase of another node.
			public ExecutionPhase Phase { get; }

			// Null for a phase of the current node.
			public EventNode Node { get; }

			public string PhaseKey { get; }

			public string Description => Node == null ? $"phase '{PhaseKey}'" : $"phase '{PhaseKey}' of node '{Node.DisplayName}'";

			public static WaitTarget ForCurrentNode(ExecutionPhase phase) => new WaitTarget(phase, null, phase.Key);

			public static WaitTarget ForOtherNode(EventNode node, string phaseKey) => new WaitTarget(null, node, phaseKey);
		}
	}
}
