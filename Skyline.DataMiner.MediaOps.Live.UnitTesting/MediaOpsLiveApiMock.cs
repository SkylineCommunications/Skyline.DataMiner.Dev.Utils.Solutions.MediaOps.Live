namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using Skyline.DataMiner.MediaOps.Live.API;

	public class MediaOpsLiveApiMock : MediaOpsLiveApi
	{
		public MediaOpsLiveApiMock()
			: base(new MediaOpsLiveSimulation().Dms.CreateConnection())
		{
		}
	}
}
