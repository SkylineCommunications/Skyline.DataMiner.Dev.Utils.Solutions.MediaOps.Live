namespace Skyline.DataMiner.MediaOps.Live.Mediation
{
	using System;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	internal static class ConnectionHandlerScript
	{
		internal static void Execute(IEngine engine, string scriptName, IConnectionHandlerRequest request, PerformanceTracker performanceTracker)
		{
			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var inputData = JsonConvert.SerializeObject(request);

				performanceTracker.AddMetadata("Script", scriptName);
				performanceTracker.AddMetadata("Input Data", inputData);

				var subScript = engine.PrepareSubScript(scriptName);
				subScript.Synchronous = true;
				subScript.ExtendedErrorInfo = true;

				subScript.SelectScriptParam("Action", Convert.ToString(request.Action));
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
