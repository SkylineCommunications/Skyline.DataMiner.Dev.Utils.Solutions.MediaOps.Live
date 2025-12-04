namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_VirtualSignalGroupLocking
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_LockSingleVSG()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var user = "TestUser";
			var reason = "Test reason";
			var jobReference = "Job123";

			// Act
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, user, reason, jobReference);

			// Assert
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.IsNotNull(state);
			Assert.AreEqual(LockState.Locked, state.LockState);
			Assert.AreEqual(user, state.LockedBy);
			Assert.AreEqual(reason, state.LockReason);
			Assert.AreEqual(jobReference, state.LockJobReference);
			Assert.IsTrue(state.IsLocked);
			Assert.IsFalse(state.IsProtected);
			Assert.IsFalse(state.IsUnlocked);
			Assert.IsGreaterThan(DateTimeOffset.MinValue, state.LockTime);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_LockMultipleVSGs()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg1 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var vsg2 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 2");
			var vsgs = new[] { vsg1, vsg2 };
			var user = "TestUser";
			var reason = "Bulk lock test";
			var jobReference = "BulkJob123";

			// Act
			api.VirtualSignalGroups.LockVirtualSignalGroups(vsgs, user, reason, jobReference);

			// Assert
			var state1 = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg1);
			var state2 = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg2);

			Assert.IsNotNull(state1);
			Assert.IsNotNull(state2);

			Assert.AreEqual(LockState.Locked, state1.LockState);
			Assert.AreEqual(LockState.Locked, state2.LockState);

			Assert.AreEqual(user, state1.LockedBy);
			Assert.AreEqual(user, state2.LockedBy);

			Assert.AreEqual(reason, state1.LockReason);
			Assert.AreEqual(reason, state2.LockReason);

			Assert.AreEqual(jobReference, state1.LockJobReference);
			Assert.AreEqual(jobReference, state2.LockJobReference);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_ProtectSingleVSG()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var user = "TestUser";
			var reason = "Protection test";
			var jobReference = "ProtectJob123";

			// Act
			api.VirtualSignalGroups.ProtectVirtualSignalGroup(vsg, user, reason, jobReference);

			// Assert
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.IsNotNull(state);
			Assert.AreEqual(LockState.Protected, state.LockState);
			Assert.AreEqual(user, state.LockedBy);
			Assert.AreEqual(reason, state.LockReason);
			Assert.AreEqual(jobReference, state.LockJobReference);
			Assert.IsFalse(state.IsLocked);
			Assert.IsTrue(state.IsProtected);
			Assert.IsFalse(state.IsUnlocked);
			Assert.IsGreaterThan(DateTimeOffset.MinValue, state.LockTime);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_ProtectMultipleVSGs()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg1 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var vsg2 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 2");
			var vsgs = new[] { vsg1, vsg2 };
			var user = "TestUser";
			var reason = "Bulk protection test";
			var jobReference = "BulkProtectJob123";

			// Act
			api.VirtualSignalGroups.ProtectVirtualSignalGroups(vsgs, user, reason, jobReference);

			// Assert
			var state1 = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg1);
			var state2 = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg2);

			Assert.IsNotNull(state1);
			Assert.IsNotNull(state2);

			Assert.AreEqual(LockState.Protected, state1.LockState);
			Assert.AreEqual(LockState.Protected, state2.LockState);

			Assert.AreEqual(user, state1.LockedBy);
			Assert.AreEqual(user, state2.LockedBy);

			Assert.AreEqual(reason, state1.LockReason);
			Assert.AreEqual(reason, state2.LockReason);

			Assert.AreEqual(jobReference, state1.LockJobReference);
			Assert.AreEqual(jobReference, state2.LockJobReference);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_UnlockSingleVSG()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			// Lock first
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "TestUser", "Test lock", "Job123");

			// Act
			api.VirtualSignalGroups.UnlockVirtualSignalGroup(vsg);

			// Assert
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.IsNotNull(state);
			Assert.AreEqual(LockState.Unlocked, state.LockState);
			Assert.IsFalse(state.IsLocked);
			Assert.IsFalse(state.IsProtected);
			Assert.IsTrue(state.IsUnlocked);
			Assert.AreEqual(DateTimeOffset.MinValue, state.LockTime);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_UnlockMultipleVSGs()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg1 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var vsg2 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 2");
			var vsgs = new[] { vsg1, vsg2 };

			// Lock first
			api.VirtualSignalGroups.LockVirtualSignalGroups(vsgs, "TestUser", "Test lock", "Job123");

			// Act
			api.VirtualSignalGroups.UnlockVirtualSignalGroups(vsgs);

			// Assert
			var state1 = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg1);
			var state2 = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg2);

			Assert.IsNotNull(state1);
			Assert.IsNotNull(state2);

			Assert.AreEqual(LockState.Unlocked, state1.LockState);
			Assert.AreEqual(LockState.Unlocked, state2.LockState);

			Assert.IsTrue(state1.IsUnlocked);
			Assert.IsTrue(state2.IsUnlocked);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_UnlockProtectedVSG()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			// Protect first
			api.VirtualSignalGroups.ProtectVirtualSignalGroup(vsg, "TestUser", "Test protect", "Job123");

			// Act
			api.VirtualSignalGroups.UnlockVirtualSignalGroup(vsg);

			// Assert
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.IsNotNull(state);
			Assert.AreEqual(LockState.Unlocked, state.LockState);
			Assert.IsTrue(state.IsUnlocked);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_CannotLockProtectedVSG()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			// Protect first
			api.VirtualSignalGroups.ProtectVirtualSignalGroup(vsg, "TestUser", "Test protect", "Job123");

			// Act & Assert
			Assert.Throws<InvalidOperationException>(() =>
			{
				api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "AnotherUser", "Try to lock", "Job456");
			});

			// Verify state is still protected
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.AreEqual(LockState.Protected, state.LockState);
			Assert.AreEqual("TestUser", state.LockedBy);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_RelockUpdatesLockInfo()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			// Lock first
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "User1", "First lock", "Job1");
			var firstState = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.AreEqual(LockState.Locked, firstState.LockState);

			// Act - Lock again with different parameters
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "User2", "Second lock", "Job2");

			// Assert - Should have the new lock info
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.IsNotNull(state);
			Assert.AreEqual(LockState.Locked, state.LockState);
			Assert.AreEqual("User2", state.LockedBy); // Updated user
			Assert.AreEqual("Second lock", state.LockReason); // Updated reason
			Assert.AreEqual("Job2", state.LockJobReference); // Updated job reference
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_NoChangeWhenLockingWithSameInfo()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			// Lock first
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "User1", "First lock", "Job1");
			var firstState = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			var firstLockTime = firstState.LockTime;

			// Act - Try to lock with same parameters
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "User1", "First lock", "Job1");

			// Assert - Should not change (lock time should remain the same)
			var secondState = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.AreEqual(LockState.Locked, secondState.LockState);
			Assert.AreEqual("User1", secondState.LockedBy);
			Assert.AreEqual("First lock", secondState.LockReason);
			Assert.AreEqual("Job1", secondState.LockJobReference);

			// Lock time should remain the same since no actual change was made
			Assert.AreEqual(firstLockTime, secondState.LockTime);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_EmptyUserThrowsExceptionForLock()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			// Act & Assert
			Assert.Throws<ArgumentException>(() =>
			{
				api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, String.Empty, "reason", "job");
			});
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_EmptyUserThrowsExceptionForProtect()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			// Act & Assert
			Assert.Throws<ArgumentException>(() =>
			{
				api.VirtualSignalGroups.ProtectVirtualSignalGroup(vsg, String.Empty, "reason", "job");
			});
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_LockWithCustomTimestamp()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var user = "TestUser";
			var reason = "Test reason";
			var jobReference = "Job123";
			var customTime = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);

			// Act
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, user, reason, jobReference, customTime);

			// Assert
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.IsNotNull(state);
			Assert.AreEqual(LockState.Locked, state.LockState);
			Assert.AreEqual(user, state.LockedBy);
			Assert.AreEqual(reason, state.LockReason);
			Assert.AreEqual(jobReference, state.LockJobReference);
			Assert.AreEqual(customTime, state.LockTime);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_LockWithoutTimestampUsesUtcNow()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var user = "TestUser";
			var reason = "Test reason";
			var jobReference = "Job123";
			var beforeLock = DateTimeOffset.UtcNow;

			// Act
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, user, reason, jobReference);

			// Assert
			var afterLock = DateTimeOffset.UtcNow;
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.IsNotNull(state);
			Assert.AreEqual(LockState.Locked, state.LockState);

			// Verify the lock time is between before and after the lock operation
			Assert.IsTrue(state.LockTime >= beforeLock, $"Lock time {state.LockTime} should be >= {beforeLock}");
			Assert.IsTrue(state.LockTime <= afterLock, $"Lock time {state.LockTime} should be <= {afterLock}");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_ProtectWithCustomTimestamp()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var user = "TestUser";
			var reason = "Protection test";
			var jobReference = "ProtectJob123";
			var customTime = new DateTimeOffset(2024, 2, 20, 14, 45, 30, TimeSpan.Zero);

			// Act
			api.VirtualSignalGroups.ProtectVirtualSignalGroup(vsg, user, reason, jobReference, customTime);

			// Assert
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.IsNotNull(state);
			Assert.AreEqual(LockState.Protected, state.LockState);
			Assert.AreEqual(user, state.LockedBy);
			Assert.AreEqual(reason, state.LockReason);
			Assert.AreEqual(jobReference, state.LockJobReference);
			Assert.AreEqual(customTime, state.LockTime);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_ProtectWithoutTimestampUsesUtcNow()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var user = "TestUser";
			var reason = "Protection test";
			var jobReference = "ProtectJob123";
			var beforeProtect = DateTimeOffset.UtcNow;

			// Act
			api.VirtualSignalGroups.ProtectVirtualSignalGroup(vsg, user, reason, jobReference);

			// Assert
			var afterProtect = DateTimeOffset.UtcNow;
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.IsNotNull(state);
			Assert.AreEqual(LockState.Protected, state.LockState);

			// Verify the lock time is between before and after the protect operation
			Assert.IsTrue(state.LockTime >= beforeProtect, $"Lock time {state.LockTime} should be >= {beforeProtect}");
			Assert.IsTrue(state.LockTime <= afterProtect, $"Lock time {state.LockTime} should be <= {afterProtect}");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_LockMultipleVSGsWithCustomTimestamp()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg1 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var vsg2 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 2");
			var vsgs = new[] { vsg1, vsg2 };
			var user = "TestUser";
			var reason = "Bulk lock test";
			var jobReference = "BulkJob123";
			var customTime = new DateTimeOffset(2024, 3, 10, 8, 15, 0, TimeSpan.Zero);

			// Act
			api.VirtualSignalGroups.LockVirtualSignalGroups(vsgs, user, reason, jobReference, customTime);

			// Assert
			var state1 = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg1);
			var state2 = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg2);

			Assert.IsNotNull(state1);
			Assert.IsNotNull(state2);

			Assert.AreEqual(LockState.Locked, state1.LockState);
			Assert.AreEqual(LockState.Locked, state2.LockState);

			Assert.AreEqual(customTime, state1.LockTime);
			Assert.AreEqual(customTime, state2.LockTime);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_ProtectMultipleVSGsWithCustomTimestamp()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg1 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var vsg2 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 2");
			var vsgs = new[] { vsg1, vsg2 };
			var user = "TestUser";
			var reason = "Bulk protection test";
			var jobReference = "BulkProtectJob123";
			var customTime = new DateTimeOffset(2024, 4, 5, 16, 20, 45, TimeSpan.Zero);

			// Act
			api.VirtualSignalGroups.ProtectVirtualSignalGroups(vsgs, user, reason, jobReference, customTime);

			// Assert
			var state1 = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg1);
			var state2 = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg2);

			Assert.IsNotNull(state1);
			Assert.IsNotNull(state2);

			Assert.AreEqual(LockState.Protected, state1.LockState);
			Assert.AreEqual(LockState.Protected, state2.LockState);

			Assert.AreEqual(customTime, state1.LockTime);
			Assert.AreEqual(customTime, state2.LockTime);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_RelockWithDifferentTimestamp()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var firstTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
			var secondTime = new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero);

			// Act - Lock first time
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "User1", "First lock", "Job1", firstTime);
			var firstState = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.AreEqual(firstTime, firstState.LockTime);

			// Act - Lock second time with different timestamp
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "User2", "Second lock", "Job2", secondTime);

			// Assert
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.IsNotNull(state);
			Assert.AreEqual(LockState.Locked, state.LockState);
			Assert.AreEqual("User2", state.LockedBy);
			Assert.AreEqual("Second lock", state.LockReason);
			Assert.AreEqual("Job2", state.LockJobReference);
			Assert.AreEqual(secondTime, state.LockTime);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupLocking_UnlockSetsLockTimeToMinValue()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var customTime = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);

			// Lock with custom time
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "TestUser", "Test lock", "Job123", customTime);
			var lockedState = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.AreEqual(customTime, lockedState.LockTime);

			// Act - Unlock
			api.VirtualSignalGroups.UnlockVirtualSignalGroup(vsg);

			// Assert
			var state = api.VirtualSignalGroupStates.GetByVirtualSignalGroup(vsg);
			Assert.IsNotNull(state);
			Assert.AreEqual(LockState.Unlocked, state.LockState);
			Assert.AreEqual(DateTimeOffset.MinValue, state.LockTime);
		}
	}
}
