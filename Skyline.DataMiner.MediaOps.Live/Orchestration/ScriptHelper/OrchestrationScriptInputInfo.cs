namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;

	public class OrchestrationScriptInputInfo
	{
		public OrchestrationScriptInputInfo()
		{
			Parameters = [];
			Elements = [];
		}

		public OrchestrationScriptInputInfo(string scriptName) : this()
		{
			ScriptName = scriptName;
		}

		public string ScriptName { get; set; }

		public Guid ProfileDefinition { get; set; }

		public List<OrchestrationScriptInputParameter> Parameters { get; }

		public List<OrchestrationScriptInputElement> Elements { get; }

		public List<ProfileInstance> GetApplicableInstances(ProfileHelper helper)
		{
			return helper.ProfileInstances.Read(ProfileInstanceExposers.AppliesToID.Equal(ProfileDefinition));
		}
	}
}
