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
			Parameters = new List<OrchestrationScriptInputParameter>();
		}

		public string ScriptName { get; set; }

		public Guid ProfileDefinition { get; set; }

		public List<OrchestrationScriptInputParameter> Parameters { get; set; }

		public List<AutomationProtocolInfo> Elements { get; set; }

		public List<ProfileInstance> GetApplicableInstances(ProfileHelper helper)
		{
			return helper.ProfileInstances.Read(ProfileInstanceExposers.AppliesToID.Equal(ProfileDefinition));
		}
	}
}
