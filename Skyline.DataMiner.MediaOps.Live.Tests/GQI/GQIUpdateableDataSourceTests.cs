namespace Skyline.DataMiner.MediaOps.Live.Tests.GQI
{
	using System;

	using Moq;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.MediaOps.Live.GQI;

	[TestClass]
	public class GQIUpdateableDataSourceTests
	{
		private GQIRow CreateRow(string key, string value = "value")
		{
			return new(key, [new GQICell { Value = value }]);
		}

		[TestMethod]
		public void GQIUpdateableDataSource_AddRow_AddsRow()
		{
			var ds = new Mock<GQIUpdateableDataSource>().Object;
			var updaterMock = new Mock<IGQIUpdater>();
			((IGQIUpdateable)ds).OnStartUpdates(updaterMock.Object);

			var row = CreateRow("1");
			ds.AddRow(row);

			updaterMock.Verify(u => u.AddRow(row), Times.Once);
		}

		[TestMethod]
		public void GQIUpdateableDataSource_UpdateRow_UpdatesIfSent()
		{
			var ds = new Mock<GQIUpdateableDataSource>().Object;
			var updaterMock = new Mock<IGQIUpdater>();
			((IGQIUpdateable)ds).OnStartUpdates(updaterMock.Object);

			var row = CreateRow("1");
			ds.AddRow(row);
			ds.UpdateRow(row);

			updaterMock.Verify(u => u.UpdateRow(row), Times.Once);
		}

		[TestMethod]
		public void GQIUpdateableDataSource_UpdateRow_DoesNotUpdateIfNotSent()
		{
			var ds = new Mock<GQIUpdateableDataSource>().Object;
			var updaterMock = new Mock<IGQIUpdater>();
			((IGQIUpdateable)ds).OnStartUpdates(updaterMock.Object);

			var row = CreateRow("1");
			ds.UpdateRow(row);

			updaterMock.Verify(u => u.UpdateRow(row), Times.Never);
		}

		[TestMethod]
		public void GQIUpdateableDataSource_UpdateRow_ThrowsIfRowRemoved()
		{
			var ds = new Mock<GQIUpdateableDataSource>().Object;
			var updaterMock = new Mock<IGQIUpdater>();
			((IGQIUpdateable)ds).OnStartUpdates(updaterMock.Object);

			var row = CreateRow("1");
			ds.AddRow(row);
			ds.RemoveRow("1");

			var updatedRow = CreateRow("1", "new");
			Assert.Throws<InvalidOperationException>(() => ds.UpdateRow(updatedRow));
		}

		[TestMethod]
		public void GQIUpdateableDataSource_AddOrUpdateRow_AddsIfNotSent()
		{
			var ds = new Mock<GQIUpdateableDataSource>().Object;
			var updaterMock = new Mock<IGQIUpdater>();
			((IGQIUpdateable)ds).OnStartUpdates(updaterMock.Object);

			var row = CreateRow("1");
			ds.AddOrUpdateRow(row);

			updaterMock.Verify(u => u.AddRow(row), Times.Once);
			updaterMock.Verify(u => u.UpdateRow(It.IsAny<GQIRow>()), Times.Never);
		}

		[TestMethod]
		public void GQIUpdateableDataSource_AddOrUpdateRow_AddsIfRemoved()
		{
			var ds = new Mock<GQIUpdateableDataSource>().Object;
			var updaterMock = new Mock<IGQIUpdater>();
			((IGQIUpdateable)ds).OnStartUpdates(updaterMock.Object);

			ds.RemoveRow("1");

			var row = CreateRow("1");
			ds.AddOrUpdateRow(row);

			updaterMock.Verify(u => u.AddRow(row), Times.Once);
			updaterMock.Verify(u => u.UpdateRow(It.IsAny<GQIRow>()), Times.Never);
		}

		[TestMethod]
		public void GQIUpdateableDataSource_AddOrUpdateRow_UpdatesIfAlreadySent()
		{
			var ds = new Mock<GQIUpdateableDataSource>().Object;
			var updaterMock = new Mock<IGQIUpdater>();
			((IGQIUpdateable)ds).OnStartUpdates(updaterMock.Object);

			var row = CreateRow("1");
			ds.AddRow(row);

			var updatedRow = CreateRow("1", "updated");
			ds.AddOrUpdateRow(updatedRow);

			updaterMock.Verify(u => u.UpdateRow(updatedRow), Times.Once);
		}

		[TestMethod]
		public void GQIUpdateableDataSource_RemoveRow_RemovesIfSent()
		{
			var ds = new Mock<GQIUpdateableDataSource>().Object;
			var updaterMock = new Mock<IGQIUpdater>();
			((IGQIUpdateable)ds).OnStartUpdates(updaterMock.Object);

			var row = CreateRow("1");
			ds.AddRow(row);
			ds.RemoveRow("1");

			updaterMock.Verify(u => u.RemoveRow("1"), Times.Once);
		}

		[TestMethod]
		public void GQIUpdateableDataSource_RemoveRow_DoesNotRemoveIfNotSent()
		{
			var ds = new Mock<GQIUpdateableDataSource>().Object;
			var updaterMock = new Mock<IGQIUpdater>();
			((IGQIUpdateable)ds).OnStartUpdates(updaterMock.Object);

			ds.RemoveRow("1");

			updaterMock.Verify(u => u.RemoveRow("1"), Times.Never);
		}
	}
}
