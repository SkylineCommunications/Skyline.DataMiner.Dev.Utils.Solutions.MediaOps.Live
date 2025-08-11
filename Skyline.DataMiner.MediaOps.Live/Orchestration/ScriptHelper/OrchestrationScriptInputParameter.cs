namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;

	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;

	public class OrchestrationScriptInputParameter
	{
		public OrchestrationScriptInputParameter(string name, Guid profileParameter)
		{
			Name = name;
			ProfileParameter = profileParameter;
		}

		public Guid ProfileParameter { get; set; }

		public bool FromProfile => ProfileParameter != Guid.Empty;

		public string Name { get; set; }

		public Parameter LinkedProfileParameter { get; set; }

		internal void LoadLinkedProfileParameter(ProfileHelper helper)
		{
			if (ProfileParameter == Guid.Empty)
			{
				throw new InvalidOperationException("There is no profile parameter ID linked to this script parameter");
			}

			List<Parameter> result = helper.ProfileParameters.Read(ParameterExposers.ID.Equal(ProfileParameter));

			if (result.Count == 0)
			{
				throw new InvalidOperationException($"No profile parameter found with ID {ProfileParameter}");
			}

			LinkedProfileParameter = result.First();
		}
	}
}
