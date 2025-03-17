namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;

	public abstract class ConnectionHandler
	{
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
				case ConnectionHandlerInputData.ScriptAction.GetSubscriptionInfo:
					HandleGetSubscriptionInfo(engine, inputData);
					break;
				case ConnectionHandlerInputData.ScriptAction.HandleParameterUpdate:
					HandleParameterUpdate(engine, inputData);
					break;
				case ConnectionHandlerInputData.ScriptAction.Connect:
					HandleConnect(engine, inputData);
					break;
				default:
					throw new InvalidOperationException($"Unknown action: '{inputData.Action}'");
			}
		}

		public abstract IEnumerable<SubscriptionInfo> GetSubscriptionInfo(IEngine engine);

		public abstract void ProcessParameterUpdate(IEngine engine, IConnectionHandlerEngine connectionEngine, ParameterUpdate update);

		public abstract void Connect(IEngine engine, IConnectionHandlerEngine connectionEngine, CreateConnectionsRequest createConnectionsRequest);

		private void HandleGetSubscriptionInfo(IEngine engine, ConnectionHandlerInputData inputData)
		{
			var subscriptionInfo = GetSubscriptionInfo(engine);

			var settings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
			};

			var serialized = JsonConvert.SerializeObject(subscriptionInfo, settings);

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
	}
}
