namespace Skyline.DataMiner.MediaOps.Live.GQI
{
	using System.Text;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.MediaOps.Live.GQI.Metrics;
	using Skyline.DataMiner.Net;

	public abstract class GQIDataSourceBase : IGQIDataSource, IGQIOnInit, IGQIOnDestroy
	{
		private ConnectionInterceptor _interceptedConnection;
		private ConnectionMetrics _connectionMetrics;

		public GQIDMS Dms { get; private set; }

		public IGQILogger Logger { get; private set; }

		public IConnection Connection => _interceptedConnection;

		public ConnectionMetrics ConnectionMetrics => _connectionMetrics;

		public abstract GQIColumn[] OnGetColumns();

		public abstract GQIPage OnGetNextPage(GetNextPageInputArgs args);

		public virtual OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_interceptedConnection = new ConnectionInterceptor(args.DMS.GetConnection());
			_connectionMetrics = new ConnectionMetrics(_interceptedConnection);

			Dms = args.DMS;
			Logger = args.Logger;

			return new OnInitOutputArgs();
		}

		public virtual GQIColumn[] GetColumns()
		{
			return OnGetColumns();
		}

		public virtual GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			return OnGetNextPage(args);
		}

		public virtual OnDestroyOutputArgs OnDestroy(OnDestroyInputArgs args)
		{
			try
			{
				var sb = new StringBuilder();
				sb.Append($"Connection metrics: ");
				sb.Append($"{_connectionMetrics.NumberOfRequests} requests, ");
				sb.Append($"{_connectionMetrics.NumberOfDomRequests} DOM requests, ");
				sb.Append($"{_connectionMetrics.NumberOfDomInstancesRetrieved} DOM instances, ");
				sb.Append($"{_connectionMetrics.AvgRequestDuration.TotalMilliseconds:F0} ms avg, ");
				sb.Append($"{_connectionMetrics.MaxRequestDuration.TotalMilliseconds:F0} ms max");

				Logger.Information(sb.ToString());

				return new OnDestroyOutputArgs();
			}
			finally
			{
				_connectionMetrics?.Dispose();
			}
		}
	}
}
