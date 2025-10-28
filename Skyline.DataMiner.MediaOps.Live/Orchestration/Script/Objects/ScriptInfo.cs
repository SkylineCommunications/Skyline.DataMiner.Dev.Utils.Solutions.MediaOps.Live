namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	using Skyline.DataMiner.Net.Profiles;

	/// <summary>
	/// Contains information about an orchestration script including profile parameters and definitions.
	/// </summary>
	public class ScriptInfo
	{
		/// <summary>
		/// Gets the dictionary of profile parameter references by name.
		/// </summary>
		[IgnoreDataMember]
		public Dictionary<string, Parameter> ProfileParameterReferences { get; } = new Dictionary<string, Parameter>();

		/// <summary>
		/// Gets the dictionary of profile parameter IDs by name.
		/// </summary>
		[DataMember]
		public Dictionary<string, Guid> ProfileParameters { get; } = new Dictionary<string, Guid>();

		/// <summary>
		/// Gets the list of profile definition references.
		/// </summary>
		[IgnoreDataMember]
		public List<ProfileDefinition> ProfileDefinitionReferences { get; } = new List<ProfileDefinition>();

		/// <summary>
		/// Gets the list of profile definition IDs.
		/// </summary>
		[DataMember]
		public List<Guid> ProfileDefinitions { get; } = new List<Guid>();
	}
}
