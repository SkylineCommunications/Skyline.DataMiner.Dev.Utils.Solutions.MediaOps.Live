namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Subscriptions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting;

	[TestClass]
	public sealed class MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver
	{
		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_ReportsChangeWhenVsgLocked()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			using var observer = new VirtualSignalGroupEndpointsObserver(api);
			observer.Subscribe();

			var receivedEvents = new List<ApiObjectsChangedEvent<VirtualSignalGroup>>();
			observer.VirtualSignalGroupsChanged += (sender, e) => receivedEvents.Add(e);

			// Act
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "TestUser", "Test lock", "Job123");

			// Assert
			Assert.HasCount(1, receivedEvents, "Expected 1 event when VSG is locked");
			Assert.IsEmpty(receivedEvents[0].Created, "Expected no created VSGs");
			Assert.IsEmpty(receivedEvents[0].Deleted, "Expected no deleted VSGs");
			Assert.HasCount(1, receivedEvents[0].Updated, "Expected 1 updated VSG");
			Assert.AreEqual(vsg.ID, receivedEvents[0].Updated[0].ID, "Expected the locked VSG to be reported as updated");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_ReportsChangeWhenVsgUnlocked()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			// Lock first
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "TestUser", "Test lock", "Job123");

			using var observer = new VirtualSignalGroupEndpointsObserver(api);
			observer.Subscribe();

			var receivedEvents = new List<ApiObjectsChangedEvent<VirtualSignalGroup>>();
			observer.VirtualSignalGroupsChanged += (sender, e) => receivedEvents.Add(e);

			// Act
			api.VirtualSignalGroups.UnlockVirtualSignalGroup(vsg);

			// Assert
			Assert.HasCount(1, receivedEvents, "Expected 1 event when VSG is unlocked");
			Assert.IsEmpty(receivedEvents[0].Created, "Expected no created VSGs");
			Assert.IsEmpty(receivedEvents[0].Deleted, "Expected no deleted VSGs");
			Assert.HasCount(1, receivedEvents[0].Updated, "Expected 1 updated VSG");
			Assert.AreEqual(vsg.ID, receivedEvents[0].Updated[0].ID, "Expected the unlocked VSG to be reported as updated");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_ReportsChangeWhenVsgProtected()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			using var observer = new VirtualSignalGroupEndpointsObserver(api);
			observer.Subscribe();

			var receivedEvents = new List<ApiObjectsChangedEvent<VirtualSignalGroup>>();
			observer.VirtualSignalGroupsChanged += (sender, e) => receivedEvents.Add(e);

			// Act
			api.VirtualSignalGroups.ProtectVirtualSignalGroup(vsg, "TestUser", "Test protect", "Job123");

			// Assert
			Assert.HasCount(1, receivedEvents, "Expected 1 event when VSG is protected");
			Assert.IsEmpty(receivedEvents[0].Created, "Expected no created VSGs");
			Assert.IsEmpty(receivedEvents[0].Deleted, "Expected no deleted VSGs");
			Assert.HasCount(1, receivedEvents[0].Updated, "Expected 1 updated VSG");
			Assert.AreEqual(vsg.ID, receivedEvents[0].Updated[0].ID, "Expected the protected VSG to be reported as updated");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_ReportsChangeForMultipleVsgsLocked()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg1 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var vsg2 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 2");
			var vsgs = new[] { vsg1, vsg2 };

			using var observer = new VirtualSignalGroupEndpointsObserver(api);
			observer.Subscribe();

			var receivedEvents = new List<ApiObjectsChangedEvent<VirtualSignalGroup>>();
			observer.VirtualSignalGroupsChanged += (sender, e) => receivedEvents.Add(e);

			// Act
			api.VirtualSignalGroups.LockVirtualSignalGroups(vsgs, "TestUser", "Bulk lock test", "BulkJob123");

			// Assert
			Assert.HasCount(1, receivedEvents, "Expected 1 event when VSGs are locked");
			Assert.IsEmpty(receivedEvents[0].Created, "Expected no created VSGs");
			Assert.IsEmpty(receivedEvents[0].Deleted, "Expected no deleted VSGs");
			Assert.HasCount(2, receivedEvents[0].Updated, "Expected 2 updated VSGs");

			var updatedIds = receivedEvents[0].Updated.Select(x => x.ID).ToList();
			CollectionAssert.Contains(updatedIds, vsg1.ID);
			CollectionAssert.Contains(updatedIds, vsg2.ID);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_ReportsChangeForMultipleVsgsUnlocked()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg1 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var vsg2 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 2");
			var vsgs = new[] { vsg1, vsg2 };

			// Lock first
			api.VirtualSignalGroups.LockVirtualSignalGroups(vsgs, "TestUser", "Test lock", "Job123");

			using var observer = new VirtualSignalGroupEndpointsObserver(api);
			observer.Subscribe();

			var receivedEvents = new List<ApiObjectsChangedEvent<VirtualSignalGroup>>();
			observer.VirtualSignalGroupsChanged += (sender, e) => receivedEvents.Add(e);

			// Act
			api.VirtualSignalGroups.UnlockVirtualSignalGroups(vsgs);

			// Assert
			Assert.HasCount(1, receivedEvents, "Expected 1 event when VSGs are unlocked");
			Assert.IsEmpty(receivedEvents[0].Created, "Expected no created VSGs");
			Assert.IsEmpty(receivedEvents[0].Deleted, "Expected no deleted VSGs");
			Assert.HasCount(2, receivedEvents[0].Updated, "Expected 2 updated VSGs");

			var updatedIds = receivedEvents[0].Updated.Select(x => x.ID).ToList();
			CollectionAssert.Contains(updatedIds, vsg1.ID);
			CollectionAssert.Contains(updatedIds, vsg2.ID);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_ReportsChangeForMultipleVsgsProtected()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg1 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");
			var vsg2 = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 2");
			var vsgs = new[] { vsg1, vsg2 };

			using var observer = new VirtualSignalGroupEndpointsObserver(api);
			observer.Subscribe();

			var receivedEvents = new List<ApiObjectsChangedEvent<VirtualSignalGroup>>();
			observer.VirtualSignalGroupsChanged += (sender, e) => receivedEvents.Add(e);

			// Act
			api.VirtualSignalGroups.ProtectVirtualSignalGroups(vsgs, "TestUser", "Bulk protect test", "BulkProtectJob123");

			// Assert
			Assert.HasCount(1, receivedEvents, "Expected 1 event when VSGs are protected");
			Assert.IsEmpty(receivedEvents[0].Created, "Expected no created VSGs");
			Assert.IsEmpty(receivedEvents[0].Deleted, "Expected no deleted VSGs");
			Assert.HasCount(2, receivedEvents[0].Updated, "Expected 2 updated VSGs");

			var updatedIds = receivedEvents[0].Updated.Select(x => x.ID).ToList();
			CollectionAssert.Contains(updatedIds, vsg1.ID);
			CollectionAssert.Contains(updatedIds, vsg2.ID);
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_CacheReflectsLockState()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			using var observer = new VirtualSignalGroupEndpointsObserver(api);
			observer.Subscribe();

			var receivedEvents = new List<ApiObjectsChangedEvent<VirtualSignalGroup>>();
			observer.VirtualSignalGroupsChanged += (sender, e) => receivedEvents.Add(e);

			// Act - Lock
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "TestUser", "Test lock", "Job123");

			// Assert - Locked state
			Assert.IsTrue(observer.Cache.VirtualSignalGroups.IsLocked(vsg), "Cache should reflect locked state");
			Assert.IsFalse(observer.Cache.VirtualSignalGroups.IsUnlocked(vsg), "Cache should not show unlocked state");
			Assert.IsFalse(observer.Cache.VirtualSignalGroups.IsProtected(vsg), "Cache should not show protected state");

			// Act - Unlock
			api.VirtualSignalGroups.UnlockVirtualSignalGroup(vsg);

			// Assert - Unlocked state
			Assert.IsFalse(observer.Cache.VirtualSignalGroups.IsLocked(vsg), "Cache should not show locked state after unlock");
			Assert.IsTrue(observer.Cache.VirtualSignalGroups.IsUnlocked(vsg), "Cache should reflect unlocked state");
			Assert.IsFalse(observer.Cache.VirtualSignalGroups.IsProtected(vsg), "Cache should not show protected state");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_CacheReflectsProtectedState()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			using var observer = new VirtualSignalGroupEndpointsObserver(api);
			observer.Subscribe();

			// Act - Protect
			api.VirtualSignalGroups.ProtectVirtualSignalGroup(vsg, "TestUser", "Test protect", "Job123");

			// Assert - Protected state
			Assert.IsFalse(observer.Cache.VirtualSignalGroups.IsLocked(vsg), "Cache should not show locked state");
			Assert.IsFalse(observer.Cache.VirtualSignalGroups.IsUnlocked(vsg), "Cache should not show unlocked state");
			Assert.IsTrue(observer.Cache.VirtualSignalGroups.IsProtected(vsg), "Cache should reflect protected state");

			// Act - Unlock
			api.VirtualSignalGroups.UnlockVirtualSignalGroup(vsg);

			// Assert - Unlocked state
			Assert.IsFalse(observer.Cache.VirtualSignalGroups.IsLocked(vsg), "Cache should not show locked state after unlock");
			Assert.IsTrue(observer.Cache.VirtualSignalGroups.IsUnlocked(vsg), "Cache should reflect unlocked state");
			Assert.IsFalse(observer.Cache.VirtualSignalGroups.IsProtected(vsg), "Cache should not show protected state after unlock");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_NoEventWhenLockingWithSameInfo()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			// Lock first
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "User1", "First lock", "Job1");

			using var observer = new VirtualSignalGroupEndpointsObserver(api);
			observer.Subscribe();

			var receivedEvents = new List<ApiObjectsChangedEvent<VirtualSignalGroup>>();
			observer.VirtualSignalGroupsChanged += (sender, e) => receivedEvents.Add(e);

			// Act - Lock again with same parameters
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "User1", "First lock", "Job1");

			// Assert - No event should be raised since there's no change
			Assert.IsEmpty(receivedEvents, "Expected no event when locking with same info");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_EventWhenRelockingWithDifferentInfo()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			// Lock first
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "User1", "First lock", "Job1");

			using var observer = new VirtualSignalGroupEndpointsObserver(api);
			observer.Subscribe();

			var receivedEvents = new List<ApiObjectsChangedEvent<VirtualSignalGroup>>();
			observer.VirtualSignalGroupsChanged += (sender, e) => receivedEvents.Add(e);

			// Act - Lock again with different parameters
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "User2", "Second lock", "Job2");

			// Assert - Event should be raised since the lock info changed
			Assert.HasCount(1, receivedEvents, "Expected 1 event when relocking with different info");
			Assert.HasCount(1, receivedEvents[0].Updated, "Expected 1 updated VSG");
			Assert.AreEqual(vsg.ID, receivedEvents[0].Updated[0].ID, "Expected the relocked VSG to be reported as updated");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_SubscribeBeforeLock()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			using var observer = new VirtualSignalGroupEndpointsObserver(api);

			// Subscribe BEFORE locking
			observer.Subscribe();

			var receivedEvents = new List<ApiObjectsChangedEvent<VirtualSignalGroup>>();
			observer.VirtualSignalGroupsChanged += (sender, e) => receivedEvents.Add(e);

			// Act
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "TestUser", "Test lock", "Job123");

			// Assert
			Assert.HasCount(1, receivedEvents, "Expected event when subscribing before lock");
			Assert.HasCount(1, receivedEvents[0].Updated, "Expected 1 updated VSG");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_NoEventWhenNotSubscribed()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			using var observer = new VirtualSignalGroupEndpointsObserver(api);

			//// Do NOT subscribe

			var receivedEvents = new List<ApiObjectsChangedEvent<VirtualSignalGroup>>();
			observer.VirtualSignalGroupsChanged += (sender, e) => receivedEvents.Add(e);

			// Act
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "TestUser", "Test lock", "Job123");

			// Assert
			Assert.IsEmpty(receivedEvents, "Expected no event when not subscribed");
		}

		[TestMethod]
		public void MediaOps_LiveApi_Tests_VirtualSignalGroupEndpointsObserver_NoEventAfterUnsubscribe()
		{
			// Arrange
			var api = new MediaOpsLiveApiMock();
			var vsg = api.VirtualSignalGroups.Query().First(x => x.Name == "Source 1");

			using var observer = new VirtualSignalGroupEndpointsObserver(api);
			observer.Subscribe();

			var receivedEvents = new List<ApiObjectsChangedEvent<VirtualSignalGroup>>();
			observer.VirtualSignalGroupsChanged += (sender, e) => receivedEvents.Add(e);

			// Unsubscribe
			observer.Unsubscribe();

			// Act
			api.VirtualSignalGroups.LockVirtualSignalGroup(vsg, "TestUser", "Test lock", "Job123");

			// Assert
			Assert.IsEmpty(receivedEvents, "Expected no event after unsubscribe");
		}
	}
}
