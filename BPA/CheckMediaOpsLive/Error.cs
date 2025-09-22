namespace CheckMediaOpsLive
{
	using System.Collections.Generic;

	public class Error
	{
		public Error(ErrorSeverity severity, string text)
		{
			Severity = severity;
			Text = text;
		}

		public ErrorSeverity Severity { get; }

		public string Text { get; }

		public object Details { get; set; }

		public enum ErrorSeverity
		{
			Warning,
			Error,
		}
	}
}
