namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Profiles;

	public class ScriptInfo
	{
		public Dictionary<string, Parameter> ProfileParameters { get; } = new Dictionary<string, Parameter>();

		public List<ProfileDefinition> ProfileDefinitions { get; } = new List<ProfileDefinition>();
	}
}
