namespace Skyline.DataMiner.MediaOps.Live.Automation.Tools
{
	using System;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;

	/// <summary>
	/// Provides a mechanism to override <see cref="Engine.SLNetRaw"/> for testing purposes.
	/// </summary>
	/// <remarks>
	/// Use <see cref="SetConnection"/> to provide a test connection and <see cref="Reset"/> to restore default behavior.
	/// </remarks>
	public static class StaticEngineConnectionProvider
	{
		private static volatile IConnection _connection;

		/// <summary>
		/// Gets the current connection.
		/// </summary>
		/// <returns>The configured connection, or <see cref="Engine.SLNetRaw"/> by default.</returns>
		public static IConnection Connection => _connection ?? Engine.SLNetRaw;

		/// <summary>
		/// Sets a custom connection for testing.
		/// </summary>
		/// <param name="connection">The connection to use.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
		public static void SetConnection(IConnection connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			_connection = connection;
		}

		/// <summary>
		/// Resets the connection to <see cref="Engine.SLNetRaw"/>.
		/// </summary>
		public static void Reset()
		{
			_connection = Engine.SLNetRaw;
		}
	}
}
