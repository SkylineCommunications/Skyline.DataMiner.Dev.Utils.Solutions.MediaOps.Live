namespace Skyline.DataMiner.MediaOps.Live.API
{
	using System;

	using Skyline.DataMiner.Net;

	public class StaticMediaOpsLiveCache
	{
		private static readonly object _lock = new();
		private static StaticMediaOpsLiveCache _instance;

		private StaticMediaOpsLiveCache(IConnection connection)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
			Api = new MediaOpsLiveApi(connection);
		}

		internal IConnection Connection { get; }

		internal MediaOpsLiveApi Api { get; }

		internal static StaticMediaOpsLiveCache GetOrCreate(Func<IConnection> connectionFactory)
		{
			if (_instance == null)
			{
				lock (_lock)
				{
					if (_instance == null)
					{
						if (connectionFactory == null)
						{
							throw new ArgumentNullException(nameof(connectionFactory));
						}

						var connection = connectionFactory();
						if (connection == null)
						{
							throw new InvalidOperationException("Connection cannot be null.");
						}

						_instance = new StaticMediaOpsLiveCache(connection);
					}
				}
			}

			return _instance;
		}

		public static void Reset()
		{
			lock (_lock)
			{
				_instance = null;
			}
		}
	}
}
