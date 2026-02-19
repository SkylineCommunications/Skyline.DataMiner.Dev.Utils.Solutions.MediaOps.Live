namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	using Skyline.DataMiner.Net.Profiles;

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
	}
}
