namespace Skyline.DataMiner.MediaOps.Live.Automation.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Automation.Logging;
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

				var createConnectionRequest = inputData.Deserialize<CreateConnectionsRequest>();
				logger.Debug($"Data: {JsonConvert.SerializeObject(createConnectionRequest, Formatting.Indented)}");

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

				var disconnectDestinationsRequest = inputData.Deserialize<DisconnectDestinationsRequest>();
				logger.Debug($"Data: {JsonConvert.SerializeObject(disconnectDestinationsRequest, Formatting.Indented)}");

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

		private string GetConnectionHandlerName()
		{
			return GetType().FullName;
		}
	}
}
