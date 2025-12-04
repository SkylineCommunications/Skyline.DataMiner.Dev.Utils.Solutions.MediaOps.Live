namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	using Skyline.DataMiner.Core.InterAppCalls.Common.Shared;

	public class SubscriptionInfo
	{
		public enum ParameterType
		{
			Standalone,
			Table,
		}

		public SubscriptionInfo(ParameterType type, int parameterId)
		{
			Type = type;
			ParameterId = parameterId;
		}

		/// <summary>
		/// Gets the type of parameter to subscribe to.
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public ParameterType Type { get; }

		/// <summary>
		/// Gets the parameter ID to subscribe to.
		/// Depending on the value of <see cref="Type"/>, this can be either a standalone parameter ID or a table parameter ID.
		/// </summary>
		public int ParameterId { get; }

		/// <summary>
		/// Gets the row key to subscribe to.
		/// When not null or empty, only changes to this specific row will be reported.
		/// This is only applicable when <see cref="Type"/> is <see cref="ParameterType.Table"/>.
		/// </summary>
		public string RowKey { get; private set; }

		/// <summary>
		/// Gets the parameter IDs of the columns to subscribe to.
		/// When not null or empty, only changes to these specific columns will be reported.
		/// It is still necessary to provide the <see cref="ParameterId"/> when using this property.
		/// This is only applicable when <see cref="Type"/> is <see cref="ParameterType.Table"/>.
		/// </summary>
		public ICollection<int> Columns { get; private set; }

		/// <summary>
		/// Creates subscription info for a standalone parameter.
		/// </summary>
		/// <param name="parameterId">The parameter ID to subscribe to.</param>
		/// <returns>The created subscription info.</returns>
		public static SubscriptionInfo StandaloneParameter(int parameterId)
		{
			return new SubscriptionInfo(ParameterType.Standalone, parameterId);
		}

		/// <summary>
		/// Creates subscription info for a table parameter.
		/// </summary>
		/// <param name="tableParameterId">The table parameter ID to subscribe to.</param>
		/// <returns>The created subscription info.</returns>
		public static SubscriptionInfo Table(int tableParameterId)
		{
			return new SubscriptionInfo(ParameterType.Table, tableParameterId);
		}

		/// <summary>
		/// Extra filter to only subscribe to a specific row in a table parameter.
		/// </summary>
		/// <param name="rowKey">The row key to subscribe to.</param>
		/// <returns>The updated subscription info.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="rowKey"/> is null or empty.</exception>
		public SubscriptionInfo FilterRowKey(string rowKey)
		{
			if (String.IsNullOrEmpty(rowKey))
			{
				throw new ArgumentException($"'{nameof(rowKey)}' cannot be null or empty.", nameof(rowKey));
			}

			RowKey = rowKey;
			return this;
		}

		/// <summary>
		/// Extra filter to only subscribe to specific columns in a table parameter.
		/// </summary>
		/// <param name="columns">The column parameter IDs to subscribe to.</param>
		/// <returns>The updated subscription info.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="columns"/> is null.</exception>
		public SubscriptionInfo FilterColumns(params ICollection<int> columns)
		{
			Columns = columns ?? throw new ArgumentNullException(nameof(columns));
			return this;
		}

		/// <summary>
		/// Extra filter to only subscribe to a specific column in a table parameter.
		/// </summary>
		/// <param name="column">The column parameter ID to subscribe to.</param>
		/// <returns>The updated subscription info.</returns>
		public SubscriptionInfo FilterColumn(int column)
		{
			Columns = [column];
			return this;
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.Append($"Type={Type}, ParameterId={ParameterId}");

			if (Type == ParameterType.Table)
			{
				if (!String.IsNullOrEmpty(RowKey))
				{
					builder.Append($", RowKey={RowKey}");
				}

				if (Columns != null && Columns.Count > 0)
				{
					builder.Append($", Columns=[{String.Join(", ", Columns)}]");
				}
			}

			return builder.ToString();
		}
	}
}
