namespace Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;

	using Parameter = Net.Profiles.Parameter;

	/// <summary>
	/// Represents a parameter input for an orchestration script.
	/// </summary>
	public class OrchestrationScriptInputParameter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationScriptInputParameter"/> class.
		/// </summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="profileParameter">The profile parameter ID.</param>
		public OrchestrationScriptInputParameter(string name, Guid profileParameter)
		{
			Name = name;
			ProfileParameter = profileParameter;
		}

		/// <summary>
		/// Gets or sets the profile parameter ID.
		/// </summary>
		public Guid ProfileParameter { get; set; }

		/// <summary>
		/// Gets a value indicating whether this parameter comes from a profile.
		/// </summary>
		public bool FromProfile => ProfileParameter != Guid.Empty;

		/// <summary>
		/// Gets or sets the parameter name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the linked profile parameter.
		/// </summary>
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
