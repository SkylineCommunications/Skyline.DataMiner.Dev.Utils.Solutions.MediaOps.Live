namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;

	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	/// <summary>
	/// Resolves the concrete <see cref="OrchestrationInputItem"/> type based on its kind discriminator.
	/// This avoids having to enable type name handling, which would allow arbitrary types to be instantiated.
	/// </summary>
	internal sealed class OrchestrationInputItemConverter : JsonConverter
	{
		public override bool CanWrite => false;

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(OrchestrationInputItem);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
			{
				return null;
			}

			var jsonObject = JObject.Load(reader);
			var kind = (string)jsonObject["kind"];

			var item = CreateItem(kind);

			using (var itemReader = jsonObject.CreateReader())
			{
				serializer.Populate(itemReader, item);
			}

			return item;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotSupportedException();
		}

		private static OrchestrationInputItem CreateItem(string kind)
		{
			switch (kind)
			{
				case OrchestrationInputKind.Group:
					return new OrchestrationInputGroup();

				case OrchestrationInputKind.Text:
					return new OrchestrationTextInputField();

				case OrchestrationInputKind.Number:
					return new OrchestrationNumberInputField();

				case OrchestrationInputKind.Discrete:
					return new OrchestrationDiscreteInputField();

				case OrchestrationInputKind.DateTime:
					return new OrchestrationDateTimeInputField();

				case OrchestrationInputKind.TimeSpan:
					return new OrchestrationTimeSpanInputField();

				case OrchestrationInputKind.Profile:
					return new OrchestrationProfileInputField();

				default:
					throw new JsonSerializationException($"Unknown orchestration input item kind '{kind}'.");
			}
		}
	}
}
