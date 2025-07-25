namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;

	public static class ConnectionHelper
	{
		public static Connection CloneConnection(IConnection baseConnection, string clientName)
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
			try
			{
				var ticket = RequestCloneTicket(baseConnection);

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

		public static bool TryCloneConnection(IConnection baseConnection, string clientName, out IConnection connection)
		{
			try
			{
				connection = CloneConnection(baseConnection, clientName);
				return true;
			}
			catch (Exception)
			{
				connection = null;
				return false;
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
