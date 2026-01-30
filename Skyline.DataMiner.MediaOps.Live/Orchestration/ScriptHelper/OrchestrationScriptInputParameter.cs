namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;

	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;

	public class OrchestrationScriptInputParameter
	{
		public OrchestrationScriptInputParameter(string name, Guid profileParameterId)
		{
			Name = name;
			ProfileParameterId = profileParameterId;
		}

		public OrchestrationScriptInputParameter(string name)
		{
			Name = name;
		}

		public string Name { get; }

		public Guid ProfileParameterId { get; }

		public Parameter LinkedProfileParameter { get; private set; }

		public bool IsFromProfile => ProfileParameterId != Guid.Empty;

		internal void LoadLinkedProfileParameter(ProfileHelper helper)
		{
			if (helper is null)
			{
				throw new ArgumentNullException(nameof(helper));
			}

			if (ProfileParameterId == Guid.Empty)
			{
				throw new InvalidOperationException("There is no profile parameter ID linked to this script parameter");
			}

			List<Parameter> result = helper.ProfileParameters.Read(ParameterExposers.ID.Equal(ProfileParameterId));

			if (result.Count == 0)
			{
				throw new InvalidOperationException($"No profile parameter found with ID {ProfileParameterId}");
			}

			LinkedProfileParameter = result.First();
		}
	}
}
