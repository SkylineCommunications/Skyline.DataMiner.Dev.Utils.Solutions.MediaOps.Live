namespace Skyline.DataMiner.MediaOps.Live.Automation.Mediation.ConnectionHandlers
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;
	using Skyline.DataMiner.MediaOps.Live.Tools;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	internal static class ConnectionHandlerScript
	{
		internal static void Execute(IConnection connection, string scriptName, IConnectionHandlerRequest request, PerformanceTracker performanceTracker)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			if (string.IsNullOrEmpty(scriptName))
			{
				throw new ArgumentException($"'{nameof(scriptName)}' cannot be null or empty.", nameof(scriptName));
			}

			if (request is null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			if (performanceTracker is null)
			{
				throw new ArgumentNullException(nameof(performanceTracker));
			}

			using (performanceTracker = new PerformanceTracker(performanceTracker))
			{
				var inputData = JsonConvert.SerializeObject(request);

				performanceTracker.AddMetadata("Script", scriptName);
				performanceTracker.AddMetadata("Input Data", inputData);

				var parameters = new Dictionary<string, string>
				{
					{ "Action", Convert.ToString(request.Action) },
					{ "Input Data", inputData },
				};

				AutomationHelper.ExecuteAutomationScript(connection, scriptName, parameters);
			}
		}
	}
}
