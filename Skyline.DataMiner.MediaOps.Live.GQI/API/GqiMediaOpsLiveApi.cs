namespace Skyline.DataMiner.MediaOps.Live.GQI.API
{
	using System;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.Net;

	public class GqiMediaOpsLiveApi : MediaOpsLiveApi
	{
		public GqiMediaOpsLiveApi(GQIDMS gqiDms, IConnection connection) : base(connection)
		{
			GqiDms = gqiDms ?? throw new ArgumentNullException(nameof(gqiDms));
		}

		public GqiMediaOpsLiveApi(GQIDMS gqiDms) : this(gqiDms, gqiDms.GetConnection())
		{
		}

		public GQIDMS GqiDms { get; }

		public override StaticMediaOpsLiveCache GetStaticCache()
		{
			return GqiDms.GetStaticMediaOpsLiveCache();
		}
	}
}
