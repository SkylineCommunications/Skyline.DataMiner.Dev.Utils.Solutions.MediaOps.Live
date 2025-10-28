namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data
{
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	/// <summary>
	/// Represents subscription information for parameter monitoring.
	/// </summary>
	public class SubscriptionInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionInfo"/> class.
		/// </summary>
		/// <param name="type">The type of parameter.</param>
		/// <param name="parameterId">The parameter ID.</param>
		public SubscriptionInfo(ParameterType type, int parameterId)
		{
			Type = type;
			ParameterId = parameterId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionInfo"/> class for table parameters.
		/// </summary>
		/// <param name="parameterId">The parameter ID.</param>
		/// <param name="rowKey">The table row key.</param>
		public SubscriptionInfo(int parameterId, string rowKey) : this(ParameterType.Table, parameterId)
		{
			RowKey = rowKey;
		}

		/// <summary>
		/// Defines the type of parameter.
		/// </summary>
		public enum ParameterType
		{
			/// <summary>
			/// Standalone parameter.
			/// </summary>
			Standalone,

			/// <summary>
			/// Table parameter.
			/// </summary>
			Table,
		}

		/// <summary>
		/// Gets the type of parameter.
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public ParameterType Type { get; }

		/// <summary>
		/// Gets the parameter ID.
		/// </summary>
		public int ParameterId { get; }

		/// <summary>
		/// Gets the table row key (only applicable for table parameters).
		/// </summary>
		public string RowKey { get; }
	}
}
