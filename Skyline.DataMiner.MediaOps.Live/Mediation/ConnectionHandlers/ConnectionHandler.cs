namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
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

			var inputData = ConnectionHandlerInputData.Load(engine);

			switch (inputData.Action)
			{
				case ScriptAction.GetSupportedElements:
					HandleGetSupportedElements(engine, inputData);
					break;
				case ScriptAction.GetSubscriptionInfo:
					HandleGetSubscriptionInfo(engine, inputData);
					break;
				case ScriptAction.HandleParameterUpdate:
					HandleParameterUpdate(engine, inputData);
					break;
				case ScriptAction.Connect:
					HandleConnect(engine, inputData);
					break;
				case ScriptAction.Disconnect:
					HandleDisconnect(engine, inputData);
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

		private void HandleGetSupportedElements(IEngine engine, ConnectionHandlerInputData inputData)
		{
			var elementInfos = inputData.Deserialize<ICollection<ElementInfo>>();

			elementInfos = GetSupportedElements(engine, elementInfos).ToList();

			var serialized = JsonConvert.SerializeObject(elementInfos, _jsonSerializerSettings);

			engine.AddScriptOutput("output", serialized);
		}

		private void HandleGetSubscriptionInfo(IEngine engine, ConnectionHandlerInputData inputData)
		{
			var subscriptionInfo = GetSubscriptionInfo(engine);

			var serialized = JsonConvert.SerializeObject(subscriptionInfo, _jsonSerializerSettings);

			engine.AddScriptOutput("output", serialized);
		}

		private void HandleParameterUpdate(IEngine engine, ConnectionHandlerInputData inputData)
		{
			var parameterUpdate = inputData.Deserialize<ParameterUpdate>();
			var connectionHandlerEngine = new ConnectionHandlerEngine(engine);

			ProcessParameterUpdate(engine, connectionHandlerEngine, parameterUpdate);
		}

		private void HandleConnect(IEngine engine, ConnectionHandlerInputData inputData)
		{
			var createConnectionRequest = inputData.Deserialize<CreateConnectionsRequest>();
			var connectionHandlerEngine = new ConnectionHandlerEngine(engine);

			Connect(engine, connectionHandlerEngine, createConnectionRequest);
		}

		private void HandleDisconnect(IEngine engine, ConnectionHandlerInputData inputData)
		{
			var disconnectDestinationsRequest = inputData.Deserialize<DisconnectDestinationsRequest>();
			var connectionHandlerEngine = new ConnectionHandlerEngine(engine);

			Disconnect(engine, connectionHandlerEngine, disconnectDestinationsRequest);
		}
	}
}
