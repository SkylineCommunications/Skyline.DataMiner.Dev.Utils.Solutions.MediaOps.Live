namespace Skyline.DataMiner.Solutions.MediaOps.Live.UnitTesting
{
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;

	public class MediaOpsLiveApiMock : MediaOpsLiveApi
	{
		public MediaOpsLiveApiMock()
			: base(new MediaOpsLiveSimulation().Dms.CreateConnection())
		{
		}
	}
}
