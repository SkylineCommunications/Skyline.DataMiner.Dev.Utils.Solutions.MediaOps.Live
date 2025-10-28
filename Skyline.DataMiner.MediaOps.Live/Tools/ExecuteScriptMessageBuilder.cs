namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Builder for constructing <see cref="ExecuteScriptMessage"/> instances with various options.
	/// </summary>
	public class ExecuteScriptMessageBuilder
	{
		private readonly ExecuteScriptMessage _message;
		private readonly List<string> _options;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExecuteScriptMessageBuilder"/> class.
		/// </summary>
		/// <param name="scriptName">The name of the script to execute.</param>
		public ExecuteScriptMessageBuilder(string scriptName)
		{
			_message = new ExecuteScriptMessage
			{
				ScriptName = scriptName,
				DataMinerID = -1,
				HostingDataMinerID = -1,
			};

			_options = [];
		}

		/// <summary>
		/// Sets whether to check sets during script execution.
		/// </summary>
		/// <param name="checkSets">True to check sets; otherwise, false.</param>
		public void SetCheckSets(bool checkSets)
		{
			_options.Add($"CHECKSETS:{(checkSets ? "TRUE" : "FALSE")}");
		}

		/// <summary>
		/// Sets whether the script execution should be synchronous.
		/// </summary>
		/// <param name="synchronous">True for synchronous execution; otherwise, false.</param>
		public void SetSynchronous(bool synchronous)
		{
			_options.Add($"DEFER:{(!synchronous ? "TRUE" : "FALSE")}");
		}

		/// <summary>
		/// Sets whether to include extended error information.
		/// </summary>
		/// <param name="extendedErrorInfo">True to include extended error information; otherwise, false.</param>
		public void SetExtendedErrorInfo(bool extendedErrorInfo)
		{
			SetOption("EXTENDED_ERROR_INFO", extendedErrorInfo);
		}

		/// <summary>
		/// Sets whether the script execution should be interactive.
		/// </summary>
		/// <param name="interactive">True for interactive execution; otherwise, false.</param>
		public void SetInteractive(bool interactive)
		{
			SetOption("INTERACTIVE", interactive);
		}

		/// <summary>
		/// Sets whether to allow information events.
		/// </summary>
		/// <param name="allowInformationEvents">True to allow information events; otherwise, false.</param>
		public void SetInformationEvent(bool allowInformationEvents)
		{
			SetOption("SKIP_STARTED_INFO_EVENT:TRUE", !allowInformationEvents);
		}

		/// <summary>
		/// Sets the script parameters.
		/// </summary>
		/// <param name="parameters">The dictionary of parameter names and values.</param>
		public void SetParameters(Dictionary<string, string> parameters)
		{
			if (parameters == null || parameters.Count == 0)
			{
				return;
			}

			foreach (var parameter in parameters)
			{
				_options.Add($"PARAMETERBYNAME:{parameter.Key}:{parameter.Value}");
			}
		}

		/// <summary>
		/// Sets the dummy elements for the script.
		/// </summary>
		/// <param name="dummies">The dictionary of dummy names and element IDs.</param>
		public void SetDummies(Dictionary<string, DmsElementId> dummies)
		{
			if (dummies == null || dummies.Count == 0)
			{
				return;
			}

			foreach (var dummy in dummies)
			{
				_options.Add($"PROTOCOLBYNAME:{dummy.Key}:{dummy.Value.AgentId}:{dummy.Value.ElementId}");
			}
		}

		/// <summary>
		/// Sets the entry point for the automation script.
		/// </summary>
		/// <param name="entryPoint">The automation entry point.</param>
		public void SetEntryPoint(AutomationEntryPoint entryPoint)
		{
			_message.CustomEntryPoint = entryPoint;
		}

		/// <summary>
		/// Builds and returns the configured <see cref="ExecuteScriptMessage"/>.
		/// </summary>
		/// <returns>The configured <see cref="ExecuteScriptMessage"/>.</returns>
		public ExecuteScriptMessage Build()
		{
			_message.Options = new SA(_options.ToArray());
			return _message;
		}

		private void SetOption(string option, bool value)
		{
			if (value)
			{
				AddOption(option);
			}
			else
			{
				RemoveOption(option);
			}
		}

		private void AddOption(string option)
		{
			if (!_options.Contains(option))
			{
				_options.Add(option);
			}
		}

		private void RemoveOption(string option)
		{
			if (_options.Contains(option))
			{
				_options.Remove(option);
			}
		}
	}
}