namespace Skyline.DataMiner.MediaOps.Live.Mediation.Data
{
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	public class SubscriptionInfo
	{
		public SubscriptionInfo(ParameterType type, int parameterId)
		{
			Type = type;
			ParameterId = parameterId;
		}

		public SubscriptionInfo(int parameterId, string rowKey) : this(ParameterType.Table, parameterId)
		{
			RowKey = rowKey;
		}

		public enum ParameterType
		{
			Standalone,
			Table,
		}

		[JsonConverter(typeof(StringEnumConverter))]
		public ParameterType Type { get; }

		public int ParameterId { get; }

		public string RowKey { get; }
	}
}
