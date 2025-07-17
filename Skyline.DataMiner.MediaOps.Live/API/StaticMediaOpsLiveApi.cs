namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.Tools;
	using Skyline.DataMiner.Net;

	public class StaticMediaOpsLiveApi
	{
		private static readonly StaticInit<MediaOpsLiveApi> _staticApi = new();

		public static MediaOpsLiveApi GetOrInitialize(IConnection connection)
		{
			if (connection == null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return _staticApi.GetOrInitialize(() => CreateStaticApi(connection));
		}

		/// <summary>
		/// Sets the static MediaOpsLiveApi instance.
		/// For unit tests or scenarios where you want to ensure the API is initialized with a specific connection.
		/// </summary>
		public static void SetInstance(MediaOpsLiveApi api)
		{
			if (api == null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			_staticApi.SetValue(api);
		}

		private static MediaOpsLiveApi CreateStaticApi(IConnection connection)
		{
			var staticConnection = ConnectionHelper.CreateConnection(connection, "MediaOps Live API Connection");
			var staticLiveApi = new MediaOpsLiveApi(staticConnection);

			return staticLiveApi;
		}
	}
}
