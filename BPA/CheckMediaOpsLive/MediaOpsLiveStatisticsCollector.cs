namespace CheckMediaOpsLive
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Enums;

	public class MediaOpsLiveStatisticsCollector
	{
		private readonly MediaOpsLiveApi _api;

		public MediaOpsLiveStatisticsCollector(MediaOpsLiveApi api)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
		}

		public MediaOpsLiveStatistics CollectStatistics()
		{
			var statistics = new MediaOpsLiveStatistics
			{
				NumberOfLevels = _api.Levels.Query().Count(),
				NumberOfTransportTypes = _api.TransportTypes.Query().Count(),
				NumberOfSourceEndpoints = _api.Endpoints.Query().Count(x => x.Role == Role.Source),
				NumberOfDestinationEndpoints = _api.Endpoints.Query().Count(x => x.Role == Role.Destination),
				NumberOfSourceVirtualSignalGroups = _api.VirtualSignalGroups.Query().Count(x => x.Role == Role.Source),
				NumberOfDestinationVirtualSignalGroups = _api.VirtualSignalGroups.Query().Count(x => x.Role == Role.Destination),
				ConnectionHandlerScripts = CollectConnectionHandlerScriptStatistics(),
			};

			return statistics;
		}

		private ICollection<ConnectionHandlerScriptStatistics> CollectConnectionHandlerScriptStatistics()
		{
			var statistics = new List<ConnectionHandlerScriptStatistics>();

			var scriptTotals =
				_api.MediationElements.GetAllElements()
					.SelectMany(e => e.DmsElement.GetTable(1000).GetData().Values)
					.Select(row => new
					{
						Script = Convert.ToString(row[2]),
						Executions = Convert.ToInt64(row[8]),
						Failed = Convert.ToInt64(row[9]),
						LastFailed = DateTime.FromOADate(Convert.ToDouble(row[10])),
					})
					.Where(x => !String.IsNullOrWhiteSpace(x.Script))
					.GroupBy(x => x.Script, StringComparer.OrdinalIgnoreCase)
					.Select(g => new
					{
						Script = g.Key,
						Executions = g.Sum(x => x.Executions),
						Failed = g.Sum(x => x.Failed),
						LastFailed = g.Max(x => x.LastFailed),
					});

			foreach (var item in scriptTotals)
			{
				statistics.Add(new ConnectionHandlerScriptStatistics
				{
					ScriptName = item.Script,
					Executions = item.Executions,
					FailedExecutions = item.Failed,
					LastFailedExecution = item.LastFailed,
				});
			}

			return statistics;
		}
	}
}
