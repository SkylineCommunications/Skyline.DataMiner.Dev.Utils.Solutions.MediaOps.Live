namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data
{
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	/// <summary>
	/// Contains information about a parameter subscription.
	/// </summary>
	public class SubscriptionInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionInfo"/> class.
		/// </summary>
		/// <param name="type">The parameter type.</param>
		/// <param name="parameterId">The parameter ID.</param>
		public SubscriptionInfo(ParameterType type, int parameterId)
		{
			Type = type;
			ParameterId = parameterId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionInfo"/> class for a table parameter.
		/// </summary>
		/// <param name="parameterId">The parameter ID.</param>
		/// <param name="rowKey">The row key for the table parameter.</param>
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
			/// A standalone parameter.
			/// </summary>
			Standalone,

			/// <summary>
			/// A table parameter.
			/// </summary>
			Table,
		}

		/// <summary>
		/// Gets the parameter type.
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public ParameterType Type { get; }

		/// <summary>
		/// Gets the parameter ID.
		/// </summary>
		public int ParameterId { get; }

		/// <summary>
		/// Gets the row key for table parameters.
		/// </summary>
		public string RowKey { get; }
	}
}
