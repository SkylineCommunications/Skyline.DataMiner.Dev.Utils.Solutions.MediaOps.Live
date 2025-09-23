namespace CheckMediaOpsLive
{
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	public class Error
	{
		public Error(ErrorSeverity severity, string text)
		{
			Severity = severity;
			Text = text;
		}

		[JsonConverter(typeof(StringEnumConverter))]
		public ErrorSeverity Severity { get; }

		public string Text { get; }

		public object Details { get; set; }
	}

	public enum ErrorSeverity
	{
		Warning,
		Error,
	}
}
