namespace Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.Net;
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

		public List<ProfileInstance> GetApplicableInstances(ProfileHelper profileHelper)
		{
			if (profileHelper is null)
			{
				throw new ArgumentNullException(nameof(profileHelper));
			}

			return profileHelper.ProfileInstances.Read(ProfileInstanceExposers.AppliesToID.Equal(ProfileDefinition));
		}

		public List<ProfileInstance> GetApplicableInstances(IConnection connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			var profileHelper = new ProfileHelper(connection.HandleMessages);

			return GetApplicableInstances(profileHelper);
		}

		public List<ProfileInstance> GetApplicableInstances(MediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			return GetApplicableInstances(api.Connection);
		}
	}
}
