namespace Skyline.DataMiner.MediaOps.Live.Tests.Mocking
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
