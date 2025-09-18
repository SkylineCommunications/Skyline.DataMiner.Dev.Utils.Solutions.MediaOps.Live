namespace Skyline.DataMiner.MediaOps.Live.API.Extensions
{
	using System;

	using Skyline.DataMiner.Net;

	public static class ConnectionExtensions
	{
		public static MediaOpsLiveApi GetMediaOpsLiveApi(this IConnection connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return new MediaOpsLiveApi(connection);
		}
	}
}
