namespace Skyline.DataMiner.MediaOps.Live.Automation.Mediation.ConnectionHandlers
{
	using System;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;

	internal class ConnectionHandlerInputData
	{
		private ConnectionHandlerInputData()
		{
		}

		public static ConnectionHandlerInputData Load(IEngine engine)
		{
			if (engine == null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			var actionInput = engine.GetScriptParam("Action").Value;

			if (!Enum.TryParse<ConnectionHandlerScriptAction>(actionInput, out var action))
			{
				throw new InvalidOperationException($"Invalid action: {actionInput}");
			}

			return new ConnectionHandlerInputData
			{
				Action = action,
				InputData = engine.GetScriptParam("Input Data").Value,
			};
		}

		public ConnectionHandlerScriptAction Action { get; private set; }

		public string InputData { get; private set; }

		internal T Deserialize<T>()
		{
			return JsonConvert.DeserializeObject<T>(InputData);
		}
	}
}
