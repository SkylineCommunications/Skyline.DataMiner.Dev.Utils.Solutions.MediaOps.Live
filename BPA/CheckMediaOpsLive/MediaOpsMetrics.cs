namespace CheckMediaOpsLive
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;

	public class MediaOpsMetrics
	{
		private readonly MediaOpsLiveApi _api;

		public MediaOpsMetrics(MediaOpsLiveApi api)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
		}

		public ICollection<Metric> Results { get; } = [];

		public void CollectStatistics()
		{
			Results.Add(new("NumberOfLevels", _api.Levels.Query().Count()));
			Results.Add(new("NumberOfTransportTypes", _api.TransportTypes.Query().Count()));
			Results.Add(new("NumberOfSourceEndpoints", _api.Endpoints.Query().Count(x => x.Role == Role.Source)));
			Results.Add(new("NumberOfDestinationEndpoints", _api.Endpoints.Query().Count(x => x.Role == Role.Destination)));
			Results.Add(new("NumberOfSourceVirtualSignalGroups", _api.VirtualSignalGroups.Query().Count(x => x.Role == Role.Source)));
			Results.Add(new("NumberOfDestinationVirtualSignalGroups", _api.VirtualSignalGroups.Query().Count(x => x.Role == Role.Destination)));

			CollectConnectionHandlerScriptExecutions();
		}

		private void CollectConnectionHandlerScriptExecutions()
		{
			var scriptTotals =
				_api.MediationElements.GetAllElements()
					.SelectMany(e => e.DmsElement.GetTable(1000).GetData().Values)
					.Select(row => new
					{
						Script = Convert.ToString(row[2]),
						Executions = Convert.ToInt64(row[8]),
						Failed = Convert.ToInt64(row[9])
					})
					.Where(x => !String.IsNullOrWhiteSpace(x.Script))
					.GroupBy(x => x.Script, StringComparer.OrdinalIgnoreCase)
					.Select(g => new
					{
						Script = g.Key,
						Executions = g.Sum(x => x.Executions),
						Failed = g.Sum(x => x.Failed)
					});

			foreach (var item in scriptTotals)
			{
				Results.Add(new Metric($"ConnectionHandlerScript_Executions[{item.Script}]", item.Executions));
				Results.Add(new Metric($"ConnectionHandlerScript_FailedExecutions[{item.Script}]", item.Failed));
			}
		}
	}
}
