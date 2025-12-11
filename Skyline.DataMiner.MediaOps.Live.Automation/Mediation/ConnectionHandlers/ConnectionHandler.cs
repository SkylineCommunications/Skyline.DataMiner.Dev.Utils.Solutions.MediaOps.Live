namespace Skyline.DataMiner.MediaOps.Live.Automation.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Automation.Logging;
	using Skyline.DataMiner.MediaOps.Live.Logging;
	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;
	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data;

	public abstract class ConnectionHandler
	{
		private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
		};

		protected ConnectionHandler()
		{
		}

		public virtual void Execute(IEngine engine)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			try
			{
				var logger = new EngineLogger(engine);
				var inputData = ConnectionHandlerInputData.Load(engine);

				switch (inputData.Action)
				{
					case ConnectionHandlerScriptAction.GetSupportedElements:
						HandleGetSupportedElements(engine, inputData, logger);
						break;
					case ConnectionHandlerScriptAction.GetSubscriptionInfo:
						HandleGetSubscriptionInfo(engine, inputData, logger);
						break;
					case ConnectionHandlerScriptAction.HandleParameterUpdate:
						HandleParameterUpdate(engine, inputData, logger);
						break;
					case ConnectionHandlerScriptAction.Connect:
						HandleConnect(engine, inputData, logger);
						break;
					case ConnectionHandlerScriptAction.Disconnect:
						HandleDisconnect(engine, inputData, logger);
						break;
					default:
						throw new InvalidOperationException($"Unknown action: '{inputData.Action}'");
				}
			}
			catch (Exception ex)
			{
				engine.AddOrUpdateScriptOutput("Exception.HasError", "true");
				engine.AddOrUpdateScriptOutput("Exception.Message", ex.Message);
				engine.AddOrUpdateScriptOutput("Exception.StackTrace", ex.StackTrace);
				engine.AddOrUpdateScriptOutput("Exception.Full", ex.ToString());
				throw;
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
				logger.Debug($"Start processing get supported elements request.");

				var elementInfos = inputData.Deserialize<ICollection<ElementInfo>>();
				elementInfos = GetSupportedElements(engine, elementInfos).ToList();

				var serialized = JsonConvert.SerializeObject(elementInfos, _jsonSerializerSettings);

				engine.AddScriptOutput("output", serialized);

				logger.Debug($"Done processing get supported elements request.\nOutput: {serialized}");
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
				logger.Debug($"Start processing subscription info request.");

				var subscriptionInfo = GetSubscriptionInfo(engine);
				var serialized = JsonConvert.SerializeObject(subscriptionInfo, _jsonSerializerSettings);

				engine.AddScriptOutput("output", serialized);

				logger.Debug($"Done processing subscription info request.\nOutput: {serialized}");
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
				var sw = System.Diagnostics.Stopwatch.StartNew();
				logger.Information($"Start processing parameter update.");

				var parameterUpdate = inputData.Deserialize<ParameterUpdate>();
				logger.Debug($"Data: {JsonConvert.SerializeObject(parameterUpdate, Formatting.Indented)}");

				var connectionHandlerEngine = new ConnectionHandlerEngine(engine, logger);
				ProcessParameterUpdate(engine, connectionHandlerEngine, parameterUpdate);

				logger.Information($"Done processing parameter update ({sw.ElapsedMilliseconds} ms).");
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
				var sw = System.Diagnostics.Stopwatch.StartNew();
				logger.Information($"Start processing connect request.");

				var createConnectionInputData = inputData.Deserialize<CreateConnectionsInputData>();
				logger.Debug($"Data: {JsonConvert.SerializeObject(createConnectionInputData, Formatting.Indented)}");

				var createConnectionRequest = TranslateInputDataToRequest(engine, createConnectionInputData);

				var connectionHandlerEngine = new ConnectionHandlerEngine(engine, logger);
				Connect(engine, connectionHandlerEngine, createConnectionRequest);

				logger.Information($"Done processing connect request ({sw.ElapsedMilliseconds} ms).");
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
				var sw = System.Diagnostics.Stopwatch.StartNew();
				logger.Information($"Start processing disconnect request.");

				var disconnectDestinationsInputData = inputData.Deserialize<DisconnectDestinationsInputData>();
				logger.Debug($"Data: {JsonConvert.SerializeObject(disconnectDestinationsInputData, Formatting.Indented)}");

				var disconnectDestinationsRequest = TranslateInputDataToRequest(engine, disconnectDestinationsInputData);

				var connectionHandlerEngine = new ConnectionHandlerEngine(engine, logger);
				Disconnect(engine, connectionHandlerEngine, disconnectDestinationsRequest);

				logger.Information($"Done processing disconnect request ({sw.ElapsedMilliseconds} ms).");
			}
			catch (Exception ex)
			{
				logger.Error(
					$"Exception in {GetConnectionHandlerName()}.{nameof(HandleDisconnect)}",
					ex);

				throw;
			}
		}

		private static CreateConnectionsRequest TranslateInputDataToRequest(IEngine engine, CreateConnectionsInputData inputData)
		{
			var cache = engine.GetMediaOpsLiveCache().VirtualSignalGroupEndpointsCache;

			var connections = inputData.Connections
				.Select(x => new CreateConnectionsRequest.ConnectionInfo(
					cache.GetEndpoint(x.SourceEndpoint.ID),
					cache.GetEndpoint(x.DestinationEndpoint.ID)))
				.ToList();

			return new CreateConnectionsRequest(connections);
		}

		private static DisconnectDestinationsRequest TranslateInputDataToRequest(IEngine engine, DisconnectDestinationsInputData inputData)
		{
			var cache = engine.GetMediaOpsLiveCache().VirtualSignalGroupEndpointsCache;

			var destinations = inputData.Destinations
				.Select(x => cache.GetEndpoint(x.ID))
				.ToList();

			return new DisconnectDestinationsRequest(destinations);
		}

		private string GetConnectionHandlerName()
		{
			return GetType().FullName;
		}
	}
}
