namespace Skyline.DataMiner.Solutions.MediaOps.Live.Plan
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API;

	public static class Extensions
	{
		public static IMediaOpsPlanHelper GetMediaOpsPlanHelper(this IMediaOpsLiveApi liveApi)
		{
			if (liveApi is null)
			{
				throw new ArgumentNullException(nameof(liveApi));
			}

			return new MediaOpsPlanHelper(liveApi);
		}
	}
}
