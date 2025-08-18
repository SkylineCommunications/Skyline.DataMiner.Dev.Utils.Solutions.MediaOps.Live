namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;

	public class ScriptInput
	{
		public ScriptInput()
			: this(new Dictionary<string, object>())
		{
		}

		public ScriptInput(Dictionary<string, object> profileParameterValues) : this(profileParameterValues, String.Empty)
		{
		}

		public ScriptInput(Dictionary<string, object> profileParameterValues, string profileInstance)
		{
			ProfileParameterValues = profileParameterValues;
			ProfileInstance = profileInstance;
		}

		public Dictionary<string, object> ProfileParameterValues { get; }

		public string ProfileInstance { get; }
	}
}