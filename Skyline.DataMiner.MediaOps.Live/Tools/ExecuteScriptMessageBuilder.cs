namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Net.Automation;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.ReportsAndDashboards;

	public class ExecuteScriptMessageBuilder
	{
		private ExecuteScriptMessage _message;
		private List<string> _options;

		public ExecuteScriptMessageBuilder(string scriptName)
		{
			_message = new ExecuteScriptMessage
			{
				ScriptName = scriptName,
				DataMinerID = -1,
				HostingDataMinerID = -1,
			};

			var options = new List<string>
			{
				$"CHECKSETS:TRUE",
				$"DEFER:TRUE",
			};
		}

		public void SetCheckSets(bool checkSets)
		{
			_options.Add($"CHECKSETS:{(checkSets ? "TRUE" : "FALSE")}");
		}

		public void SetSynchronous(bool synchronous)
		{
			_options.Add($"DEFER:{(!synchronous ? "TRUE" : "FALSE")}");
		}

		public void SetExtendedErrorInfo(bool extendedErrorInfo)
		{
			SetOption("EXTENDED_ERROR_INFO", extendedErrorInfo);
		}

		public void SetInteractive(bool interactive)
		{
			SetOption("INTERACTIVE", interactive);
		}

		public void SetInformationEvent(bool allowInformationEvents)
		{
			SetOption("SKIP_STARTED_INFO_EVENT:TRUE", !allowInformationEvents);
		}

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

		public void SetEntryPoint(AutomationEntryPoint entryPoint)
		{
			_message.CustomEntryPoint = entryPoint;
		}

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