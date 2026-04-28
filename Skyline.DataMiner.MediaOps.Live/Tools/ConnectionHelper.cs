namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tools
{
	using System;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;

	internal static class ConnectionHelper
	{
		public static IConnection CloneConnection(IConnection baseConnection, string clientName)
		{
			if (baseConnection == null)
			{
				throw new ArgumentNullException(nameof(baseConnection));
			}

			if (String.IsNullOrWhiteSpace(clientName))
			{
				throw new ArgumentException($"'{nameof(clientName)}' cannot be null or whitespace.", nameof(clientName));
			}

			var attributes = ConnectionAttributes.AllowMessageThrottling;

			// With ProtoBuf enabled, the first time it takes >10 seconds longer to create the connection (InitProtobuf method).
			attributes |= ConnectionAttributes.NoProtoBufSerialization;

			try
			{
				var ticket = RequestCloneTicket(baseConnection);

				if (ticket == "<simulated connection>")
				{
					// Simulated connection, return the same connection.
					return baseConnection;
				}

				var connection2 = ConnectionSettings.GetConnection("localhost", attributes);
				connection2.ClientApplicationName = clientName;
				connection2.AuthenticateUsingTicket(ticket);
				connection2.Subscribe();

				return connection2;
			}
			catch (Exception ex)
			{
				throw new DataMinerException("Failed to setup a connection with the DataMiner Agent: " + ex.Message, ex);
			}
		}

		public static bool IsManagedDataMinerModule(IConnection connection)
		{
			if (connection == null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return connection is IConnectionConfig connectionConfig &&
				connectionConfig.Attributes.HasFlag(ConnectionAttributes.ManagedDataMinerModule);
		}

		/// <summary>
		/// Requests a one time ticket that can be used to authenticate another connection.
		/// </summary>
		/// <returns>Ticket.</returns>
		private static string RequestCloneTicket(IConnection baseConnection)
		{
			var requestInfo = new RequestTicketMessage(TicketType.Authentication, []);
			var ticketInfo = baseConnection.HandleSingleResponseMessage(requestInfo) as TicketResponseMessage;

			if (ticketInfo == null)
			{
				throw new DataMinerException("Did not receive ticket.");
			}

			return ticketInfo.Ticket;
		}
	}
}
