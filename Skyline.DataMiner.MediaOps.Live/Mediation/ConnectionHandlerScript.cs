namespace Skyline.DataMiner.MediaOps.Live.Mediation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
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

			if (String.IsNullOrEmpty(scriptName))
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

		internal static string FindScriptForElement(IEngine engine, IDmsElement element)
		{
			if (engine is null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			if (element is null)
			{
				throw new ArgumentNullException(nameof(element));
			}

			var hostingAgentId = element.Host.Id;

			var mediationElement = engine.FindElementsByProtocol(Constants.MediationProtocolName)
				.FirstOrDefault(e => e.RawInfo.HostingAgentID == hostingAgentId);

			if (mediationElement == null)
			{
				throw new InvalidOperationException($"Couldn't find MediaOps mediation element on hosting agent {hostingAgentId}");
			}

			var elementKey = element.DmsElementId.Value;
			var script = Convert.ToString(mediationElement.GetParameterByPrimaryKey(1003, elementKey));

			if (String.IsNullOrEmpty(script))
			{
				throw new InvalidOperationException($"No connection handler script found for element '{elementKey}'.");
			}

			return script;
		}
	}
}
