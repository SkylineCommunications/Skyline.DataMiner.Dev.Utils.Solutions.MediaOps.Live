namespace Skyline.DataMiner.MediaOps.Live.GQI
{
	using System.Text;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.MediaOps.Live.GQI.Metrics;
	using Skyline.DataMiner.Net;

	public abstract class GQIDataSourceBase : IGQIDataSource, IGQIOnInit, IGQIOnDestroy
	{
		private GQIDMS _dms;
		private IGQILogger _logger;
		private ConnectionInterceptor _interceptedConnection;
		private ConnectionMetrics _connectionMetrics;

		public GQIDMS Dms => _dms;

		public IConnection Connection => _interceptedConnection;

		public virtual OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_dms = args.DMS;
			_logger = args.Logger;
			_interceptedConnection = new ConnectionInterceptor(args.DMS.GetConnection());
			_connectionMetrics = new ConnectionMetrics(_interceptedConnection);

			return new OnInitOutputArgs();
		}

		public abstract GQIColumn[] GetColumns();

		public abstract GQIPage GetNextPage(GetNextPageInputArgs args);

		public virtual OnDestroyOutputArgs OnDestroy(OnDestroyInputArgs args)
		{
			var sb = new StringBuilder();
			sb.Append($"Connection metrics: ");
			sb.Append($"{_connectionMetrics.NumberOfRequests} requests, ");
			sb.Append($"{_connectionMetrics.NumberOfDomRequests} DOM requests, ");
			sb.Append($"{_connectionMetrics.NumberOfDomInstancesRetrieved} DOM instances, ");
			sb.Append($"{_connectionMetrics.AvgRequestDuration.TotalMilliseconds:F0} ms avg, ");
			sb.Append($"{_connectionMetrics.MaxRequestDuration.TotalMilliseconds:F0} ms max");

			_logger.Information(sb.ToString());

			return new OnDestroyOutputArgs();
		}
	}
}
