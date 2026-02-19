namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;

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

		public ProfileDefinition ProfileDefinition { get; set; }

		public ICollection<OrchestrationScriptInputParameter> Parameters { get; }

		public ICollection<OrchestrationScriptInputElement> Elements { get; }

		public ICollection<ProfileInstance> GetApplicableProfileInstances(ProfileHelper profileHelper)
		{
			if (profileHelper is null)
			{
				throw new ArgumentNullException(nameof(profileHelper));
			}

			if (ProfileDefinition == null)
			{
				return [];
			}

			var filter = ProfileInstanceExposers.AppliesToID.Equal(ProfileDefinition.ID);

			return profileHelper.ProfileInstances.Read(filter);
		}

		public ICollection<ProfileInstance> GetApplicableProfileInstances(IConnection connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			var profileHelper = new ProfileHelper(connection.HandleMessages);

			return GetApplicableProfileInstances(profileHelper);
		}

		public ICollection<ProfileInstance> GetApplicableProfileInstances(IMediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			return GetApplicableProfileInstances(api.Connection);
		}
	}
}
