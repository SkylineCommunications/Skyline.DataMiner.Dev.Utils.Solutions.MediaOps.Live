namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data
{
	using System;

	/// <summary>
	/// Configuration settings for a connection handler script.
	/// </summary>
	public class ConnectionHandlerConfiguration
	{
		public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

		/// <summary>
		/// Gets or sets the timeout for connect operations.
		/// </summary>
		public TimeSpan? ConnectTimeout { get; set; }

		/// <summary>
		/// Gets or sets the timeout for disconnect operations.
		/// </summary>
		public TimeSpan? DisconnectTimeout { get; set; }

		/// <summary>
		/// Gets the default configuration.
		/// </summary>
		public static ConnectionHandlerConfiguration Default { get; } = new()
		{
			ConnectTimeout = DefaultTimeout,
			DisconnectTimeout = DefaultTimeout,
		};
	}
}
