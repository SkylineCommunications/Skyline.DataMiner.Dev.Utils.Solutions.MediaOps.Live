namespace Skyline.DataMiner.MediaOps.Live.Automation.Take
{
	using System;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Automation.API;
	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	public class EngineTakeHelper : TakeHelper
	{
		internal EngineTakeHelper(EngineMediaOpsLiveApi api) : base(api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			Engine = api.Engine;
		}

		public IEngine Engine { get; }

		protected override void ExecuteConnectionHandlerScript(string script, ConnectionHandlerScriptAction action, IConnectionHandlerInputData inputData, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var inputDataSerialized = JsonConvert.SerializeObject(inputData);
				performanceTracker.AddMetadata("Script", script);
				performanceTracker.AddMetadata("Input Data", inputDataSerialized);

				var subScript = Engine.PrepareSubScript(script);
				subScript.Synchronous = true;
				subScript.ExtendedErrorInfo = true;

				subScript.SelectScriptParam("Action", Convert.ToString(action));
				subScript.SelectScriptParam("Input Data", inputDataSerialized);

				subScript.StartScript();

				if (subScript.HadError)
				{
					throw new DataMinerException("Script execution failed: " + String.Join(", ", subScript.GetErrorMessages()));
				}
			}
		}
	}
}
