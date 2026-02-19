namespace Skyline.DataMiner.Solutions.MediaOps.Live.GQI.API
{
	using System;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Caching;

	public class GqiMediaOpsLiveApi : MediaOpsLiveApi, IGqiMediaOpsLiveApi
	{
		public GqiMediaOpsLiveApi(GQIDMS gqiDms, IConnection connection) : base(connection)
		{
			GqiDms = gqiDms ?? throw new ArgumentNullException(nameof(gqiDms));
		}

		public GqiMediaOpsLiveApi(GQIDMS gqiDms) : this(gqiDms, gqiDms.GetConnection())
		{
		}

		public GQIDMS GqiDms { get; }

		public override MediaOpsLiveCache GetCache()
		{
			return GqiDms.GetMediaOpsLiveCache();
		}
	}
}
