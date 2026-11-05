namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests.Automation.Orchestration
{
	using System;
	using System.Threading;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases;

	[TestClass]
	public sealed class PhaseExecutionGuardTests
	{
		private readonly Guid _jobInfoId = Guid.NewGuid();
		private readonly Guid _eventId = Guid.NewGuid();

		[TestMethod]
		public void PhaseExecutionGuard_SecondExecutionForTheSameNode_IsRefusedUntilTheFirstReleases()
		{
			using (var holder = new GuardHolder(_jobInfoId, _eventId, "node-a"))
			{
				Assert.IsTrue(holder.Acquired);
				Assert.IsFalse(TryAcquireOnOtherThread(_jobInfoId, _eventId, "node-a"));
			}

			Assert.IsTrue(TryAcquireOnOtherThread(_jobInfoId, _eventId, "node-a"));
		}

		[TestMethod]
		public void PhaseExecutionGuard_OtherNodeOrEvent_IsNotBlocked()
		{
			using (new GuardHolder(_jobInfoId, _eventId, "node-a"))
			{
				Assert.IsTrue(TryAcquireOnOtherThread(_jobInfoId, _eventId, "node-b"));
				Assert.IsTrue(TryAcquireOnOtherThread(_jobInfoId, Guid.NewGuid(), "node-a"));
			}
		}

		[TestMethod]
		public void PhaseExecutionGuard_OwnerThatEndsWithoutReleasing_DoesNotBlockTheNextExecution()
		{
			var thread = new Thread(() => PhaseExecutionGuard.TryAcquire(_jobInfoId, _eventId, "node-a", out _));
			thread.Start();
			thread.Join();

			Assert.IsTrue(TryAcquireOnOtherThread(_jobInfoId, _eventId, "node-a"));
		}

		[TestMethod]
		public void PhaseExecutionGuard_NodeIdWithSpecialCharacters_GivesAValidName()
		{
			string name = PhaseExecutionGuard.GetName(_jobInfoId, _eventId, @"Encoder\1 / ""main""");

			Assert.IsFalse(name.Substring(@"Local\".Length).Contains(@"\"));
			Assert.IsTrue(PhaseExecutionGuard.TryAcquire(_jobInfoId, _eventId, @"Encoder\1 / ""main""", out var guard));
			guard.Dispose();
		}

		[TestMethod]
		public void PhaseExecutionGuard_Dispose_CanBeCalledTwice()
		{
			Assert.IsTrue(PhaseExecutionGuard.TryAcquire(_jobInfoId, _eventId, "node-a", out var guard));

			guard.Dispose();
			guard.Dispose();
		}

		private static bool TryAcquireOnOtherThread(Guid jobInfoId, Guid eventId, string nodeId)
		{
			bool acquired = false;
			var thread = new Thread(() =>
			{
				acquired = PhaseExecutionGuard.TryAcquire(jobInfoId, eventId, nodeId, out var guard);
				guard?.Dispose();
			});

			thread.Start();
			thread.Join();
			return acquired;
		}

		// Holds the guard on a dedicated thread, since a mutex must be released by the thread that owns it.
		private sealed class GuardHolder : IDisposable
		{
			private readonly ManualResetEventSlim _acquired = new ManualResetEventSlim();
			private readonly ManualResetEventSlim _release = new ManualResetEventSlim();
			private readonly Thread _thread;

			public GuardHolder(Guid jobInfoId, Guid eventId, string nodeId)
			{
				_thread = new Thread(() =>
				{
					Acquired = PhaseExecutionGuard.TryAcquire(jobInfoId, eventId, nodeId, out var guard);
					_acquired.Set();
					_release.Wait();
					guard?.Dispose();
				});

				_thread.Start();
				_acquired.Wait();
			}

			public bool Acquired { get; private set; }

			public void Dispose()
			{
				_release.Set();
				_thread.Join();
				_acquired.Dispose();
				_release.Dispose();
			}
		}
	}
}
