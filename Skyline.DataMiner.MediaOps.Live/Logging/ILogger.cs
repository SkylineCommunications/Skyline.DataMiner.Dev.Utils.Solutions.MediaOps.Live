namespace Skyline.DataMiner.MediaOps.Live.Logging
{
	using System;

	/// <summary>
	/// Interface for logging functionality.
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// Logs a message with the specified log type.
		/// </summary>
		/// <param name="message">The message to log.</param>
		/// <param name="type">The log type/severity level. Defaults to Information.</param>
		void Log(string message, LogType type = LogType.Information);

		/// <summary>
		/// Logs a debug message.
		/// </summary>
		/// <param name="message">The debug message to log.</param>
		void Debug(string message);

		/// <summary>
		/// Logs an information message.
		/// </summary>
		/// <param name="message">The information message to log.</param>
		void Information(string message);

		/// <summary>
		/// Logs a warning message.
		/// </summary>
		/// <param name="message">The warning message to log.</param>
		void Warning(string message);

		/// <summary>
		/// Logs an error message with an optional exception.
		/// </summary>
		/// <param name="message">The error message to log.</param>
		/// <param name="exception">Optional exception associated with the error.</param>
		void Error(string message, Exception exception = null);
	}
}
