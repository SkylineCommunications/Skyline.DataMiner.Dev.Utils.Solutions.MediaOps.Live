namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

	public class OrchestrationScriptInfo
	{
		[IgnoreDataMember]
		public Dictionary<Guid, Parameter> ProfileParameterReferences { get; } = new Dictionary<Guid, Parameter>();

		[DataMember]
		public Dictionary<string, Guid> ProfileParametersIdByName { get; } = new Dictionary<string, Guid>();

		[IgnoreDataMember]
		public List<ProfileDefinition> ProfileDefinitionReferences { get; } = new List<ProfileDefinition>();

		[DataMember]
		public List<Guid> ProfileDefinitions { get; } = new List<Guid>();

		/// <summary>
		/// Gets or sets the input items the script requires, evaluated for the values that were already provided.
		/// This is <see langword="null"/> for scripts that do not declare dynamic inputs.
		/// </summary>
		[DataMember]
		public OrchestrationInputDefinition InputDefinition { get; set; }
	}
}
