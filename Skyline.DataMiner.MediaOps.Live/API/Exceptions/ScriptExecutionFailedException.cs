namespace Skyline.DataMiner.MediaOps.Live.API.Exceptions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages;

	public class ScriptExecutionFailedException : Exception
	{
		public ScriptExecutionFailedException()
		{
		}

		public ScriptExecutionFailedException(string message) : base(message)
		{
		}

		public ScriptExecutionFailedException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public ScriptExecutionFailedException(string scriptName, ExecuteScriptResponseMessage response)
			: base(GenerateMessage(scriptName, response?.ErrorMessages))
		{
			ScriptName = scriptName ?? throw new ArgumentNullException(nameof(scriptName));
			Response = response ?? throw new ArgumentNullException(nameof(response));
		}

		public string ScriptName { get; }

		public ExecuteScriptResponseMessage Response { get; }

		public int ErrorCode => Response?.ErrorCode ?? 0;

		public string[] ErrorMessages => Response?.ErrorMessages ?? [];

		public IDictionary<string, string> ScriptOutput => Response?.ScriptOutput ?? [];

		private static string GenerateMessage(string scriptName, string[] errorMessages)
		{
			var message = $"Script '{scriptName}' execution failed";

			if (errorMessages != null && errorMessages.Length > 0)
			{
				message += ": " + String.Join(", ", errorMessages);
			}

			return message;
		}

		public override string ToString()
		{
			return $"{base.ToString()}, ScriptName='{ScriptName}', ErrorMessages=[{String.Join(", ", ErrorMessages)}]";
		}
	}
}
