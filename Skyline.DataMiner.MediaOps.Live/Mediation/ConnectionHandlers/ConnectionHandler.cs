namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Logging;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;

	public abstract class ConnectionHandler
	{
		private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
		};

		protected ConnectionHandler()
		{
		}

		public void Execute(IEngine engine)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			var logger = new EngineLogger(engine);

			var inputData = ConnectionHandlerInputData.Load(engine);

			switch (inputData.Action)
			{
				case ScriptAction.GetSupportedElements:
					HandleGetSupportedElements(engine, inputData, logger);
					break;
				case ScriptAction.GetSubscriptionInfo:
					HandleGetSubscriptionInfo(engine, inputData, logger);
					break;
				case ScriptAction.HandleParameterUpdate:
					HandleParameterUpdate(engine, inputData, logger);
					break;
				case ScriptAction.Connect:
					HandleConnect(engine, inputData, logger);
					break;
				case ScriptAction.Disconnect:
					HandleDisconnect(engine, inputData, logger);
					break;
				default:
					throw new InvalidOperationException($"Unknown action: '{inputData.Action}'");
			}
		}

		public abstract IEnumerable<ElementInfo> GetSupportedElements(IEngine engine, IEnumerable<ElementInfo> elements);

		public abstract IEnumerable<SubscriptionInfo> GetSubscriptionInfo(IEngine engine);

		public abstract void ProcessParameterUpdate(IEngine engine, IConnectionHandlerEngine connectionEngine, ParameterUpdate update);

		public abstract void Connect(IEngine engine, IConnectionHandlerEngine connectionEngine, CreateConnectionsRequest createConnectionsRequest);

		public abstract void Disconnect(IEngine engine, IConnectionHandlerEngine connectionEngine, DisconnectDestinationsRequest disconnectDestinationsRequest);

		private void HandleGetSupportedElements(IEngine engine, ConnectionHandlerInputData inputData, ILogger logger)
		{
			try
			{
				var elementInfos = inputData.Deserialize<ICollection<ElementInfo>>();

				elementInfos = GetSupportedElements(engine, elementInfos).ToList();

				var serialized = JsonConvert.SerializeObject(elementInfos, _jsonSerializerSettings);

				engine.AddScriptOutput("output", serialized);
			}
			catch (Exception ex)
			{
				logger.Error(
					$"Exception in {GetConnectionHandlerName()}.{nameof(HandleGetSupportedElements)}",
					ex);

				throw;
			}
		}

		private void HandleGetSubscriptionInfo(IEngine engine, ConnectionHandlerInputData inputData, ILogger logger)
		{
			try
			{
				var subscriptionInfo = GetSubscriptionInfo(engine);

				var serialized = JsonConvert.SerializeObject(subscriptionInfo, _jsonSerializerSettings);

				engine.AddScriptOutput("output", serialized);
			}
			catch (Exception ex)
			{
				logger.Error(
					$"Exception in {GetConnectionHandlerName()}.{nameof(HandleGetSubscriptionInfo)}",
					ex);

				throw;
			}
		}

		private void HandleParameterUpdate(IEngine engine, ConnectionHandlerInputData inputData, ILogger logger)
		{
			try
			{
				var parameterUpdate = inputData.Deserialize<ParameterUpdate>();

				logger.Information($"Starting processing parameter update.");
				logger.Debug($"Data: {JsonConvert.SerializeObject(parameterUpdate, Formatting.Indented)}");

				var connectionHandlerEngine = new ConnectionHandlerEngine(engine, logger);
				ProcessParameterUpdate(engine, connectionHandlerEngine, parameterUpdate);

				logger.Information("Done processing parameter update.");
			}
			catch (Exception ex)
			{
				logger.Error(
					$"Exception in {GetConnectionHandlerName()}.{nameof(HandleParameterUpdate)}",
					ex);

				throw;
			}
		}

		private void HandleConnect(IEngine engine, ConnectionHandlerInputData inputData, ILogger logger)
		{
			try
			{
				var createConnectionRequest = inputData.Deserialize<CreateConnectionsRequest>();

				logger.Information($"Starting processing connect request for {createConnectionRequest.Connections.Count} connections.");
				logger.Debug($"Data: {JsonConvert.SerializeObject(createConnectionRequest, Formatting.Indented)}");

				var connectionHandlerEngine = new ConnectionHandlerEngine(engine, logger);
				Connect(engine, connectionHandlerEngine, createConnectionRequest);

				logger.Information("Done processing connect request.");
			}
			catch (Exception ex)
			{
				logger.Error(
					$"Exception in {GetConnectionHandlerName()}.{nameof(HandleConnect)}",
					ex);

				throw;
			}
		}

		private void HandleDisconnect(IEngine engine, ConnectionHandlerInputData inputData, ILogger logger)
		{
			try
			{
				var disconnectDestinationsRequest = inputData.Deserialize<DisconnectDestinationsRequest>();

				logger.Information($"Starting processing disconnect request for {disconnectDestinationsRequest.Destinations.Count} destinations.");
				logger.Debug($"Data: {JsonConvert.SerializeObject(disconnectDestinationsRequest, Formatting.Indented)}");

				var connectionHandlerEngine = new ConnectionHandlerEngine(engine, logger);
				Disconnect(engine, connectionHandlerEngine, disconnectDestinationsRequest);

				logger.Information("Done processing disconnect request.");
			}
			catch (Exception ex)
			{
				logger.Error(
					$"Exception in {GetConnectionHandlerName()}.{nameof(HandleDisconnect)}",
					ex);

				throw;
			}
		}

		private string GetConnectionHandlerName()
		{
			return GetType().FullName;
		}
	}
}
