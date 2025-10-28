namespace Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;

	/// <summary>
	/// Represents the input information for an orchestration script.
	/// </summary>
	public class OrchestrationScriptInputInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationScriptInputInfo"/> class.
		/// </summary>
		public OrchestrationScriptInputInfo()
		{
			Parameters = [];
			Elements = [];
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationScriptInputInfo"/> class.
		/// </summary>
		/// <param name="scriptName">The name of the orchestration script.</param>
		public OrchestrationScriptInputInfo(string scriptName) : this()
		{
			ScriptName = scriptName;
		}

		/// <summary>
		/// Gets or sets the name of the orchestration script.
		/// </summary>
		public string ScriptName { get; set; }

		/// <summary>
		/// Gets or sets the profile definition ID.
		/// </summary>
		public Guid ProfileDefinition { get; set; }

		/// <summary>
		/// Gets the list of input parameters.
		/// </summary>
		public List<OrchestrationScriptInputParameter> Parameters { get; }

		/// <summary>
		/// Gets the list of input elements.
		/// </summary>
		public List<OrchestrationScriptInputElement> Elements { get; }

		/// <summary>
		/// Gets the list of applicable profile instances for this script.
		/// </summary>
		/// <param name="helper">The profile helper.</param>
		/// <returns>A list of applicable profile instances.</returns>
		public List<ProfileInstance> GetApplicableInstances(ProfileHelper helper)
		{
			return helper.ProfileInstances.Read(ProfileInstanceExposers.AppliesToID.Equal(ProfileDefinition));
		}
	}
}
