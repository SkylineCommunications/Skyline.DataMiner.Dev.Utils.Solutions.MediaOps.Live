namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;
	using System.Globalization;

	using Newtonsoft.Json;

	// Writes values as plain JSON strings and numbers.
	internal sealed class OrchestrationInputValueJsonConverter : JsonConverter<OrchestrationInputValue>
	{
		public override void WriteJson(JsonWriter writer, OrchestrationInputValue value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
			}
			else if (value.IsNumber)
			{
				writer.WriteValue(value.Number);
			}
			else
			{
				writer.WriteValue(value.Text);
			}
		}

		public override OrchestrationInputValue ReadJson(JsonReader reader, Type objectType, OrchestrationInputValue existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			switch (reader.TokenType)
			{
				case JsonToken.Null:
				case JsonToken.Undefined:
					return null;

				case JsonToken.String:
					return OrchestrationInputValue.FromText(Convert.ToString(reader.Value, CultureInfo.InvariantCulture));

				// Newtonsoft turns date-like strings into dates unless DateParseHandling is None.
				case JsonToken.Date:
					return OrchestrationInputValue.FromText(reader.Value is DateTimeOffset offset
						? offset.ToString("o", CultureInfo.InvariantCulture)
						: ((DateTime)reader.Value).ToString("o", CultureInfo.InvariantCulture));

				case JsonToken.Integer:
				case JsonToken.Float:
					return OrchestrationInputValue.FromNumber(Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture));

				default:
					throw new JsonSerializationException($"Unexpected token {reader.TokenType} for an orchestration input value.");
			}
		}
	}
}
