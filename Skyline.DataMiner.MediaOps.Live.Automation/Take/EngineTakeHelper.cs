namespace Skyline.DataMiner.MediaOps.Live.Automation.Take
{
	using System;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;
	using Skyline.DataMiner.MediaOps.Live.Take;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	public class EngineTakeHelper : TakeHelper
	{
		private readonly IEngine _engine;

		internal EngineTakeHelper(IEngine engine, MediaOpsLiveApi api) : base(api)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		internal EngineTakeHelper(IEngine engine) : this(engine, engine.GetMediaOpsLiveApi())
		{
		}

		protected override void ExecuteConnectionHandlerScript(string script, ConnectionHandlerScriptAction action, IConnectionHandlerRequest request, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var inputData = JsonConvert.SerializeObject(request);
				performanceTracker.AddMetadata("Script", script);
				performanceTracker.AddMetadata("Input Data", inputData);

				var subScript = _engine.PrepareSubScript(script);
				subScript.Synchronous = true;
				subScript.ExtendedErrorInfo = true;

				subScript.SelectScriptParam("Action", Convert.ToString(action));
				subScript.SelectScriptParam("Input Data", inputData);

				subScript.StartScript();

				if (subScript.HadError)
				{
					throw new InvalidOperationException(String.Join(@"\r\n", subScript.GetErrorMessages()));
				}
			}
		}
	}
}
