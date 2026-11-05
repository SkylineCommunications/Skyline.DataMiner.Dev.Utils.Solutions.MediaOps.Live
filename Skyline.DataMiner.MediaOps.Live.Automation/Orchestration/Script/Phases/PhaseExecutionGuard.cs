namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;
	using System.Linq;
	using System.Security.Cryptography;
	using System.Text;
	using System.Threading;

	/// <summary>
	/// Ensures the phases of a node are executed by only one execution at a time per job and event.
	/// Backed by a named mutex, so it covers every script execution in the process and is released when the process stops.
	/// </summary>
	internal sealed class PhaseExecutionGuard : IDisposable
	{
		private readonly Mutex _mutex;
		private bool _disposed;

		private PhaseExecutionGuard(Mutex mutex)
		{
			_mutex = mutex;
		}

		/// <summary>
		/// Tries to take ownership of the phases of the node, without waiting.
		/// </summary>
		/// <returns><see langword="false"/> when another execution currently owns them.</returns>
		internal static bool TryAcquire(Guid jobInfoId, Guid eventId, string nodeId, out PhaseExecutionGuard guard)
		{
			if (String.IsNullOrEmpty(nodeId))
			{
				throw new ArgumentException($"'{nameof(nodeId)}' cannot be null or empty.", nameof(nodeId));
			}

			var mutex = new Mutex(false, GetName(jobInfoId, eventId, nodeId));
			bool acquired;

			try
			{
				acquired = mutex.WaitOne(TimeSpan.Zero);
			}
			catch (AbandonedMutexException)
			{
				// The previous owner ended without releasing, for example because its thread was aborted.
				acquired = true;
			}

			if (!acquired)
			{
				mutex.Dispose();
				guard = null;
				return false;
			}

			guard = new PhaseExecutionGuard(mutex);
			return true;
		}

		/// <summary>
		/// Releases ownership. Must be called on the thread that acquired the guard.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_mutex.ReleaseMutex();
			_mutex.Dispose();
		}

		internal static string GetName(Guid jobInfoId, Guid eventId, string nodeId)
		{
			// Node IDs are free text, so they are hashed to keep the mutex name valid and short.
			using (var sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(nodeId));
				string nodeHash = String.Concat(hash.Take(16).Select(b => b.ToString("x2")));

				return $@"Local\MediaOpsLive.OrchestrationPhases.{jobInfoId:N}.{eventId:N}.{nodeHash}";
			}
		}
	}
}
