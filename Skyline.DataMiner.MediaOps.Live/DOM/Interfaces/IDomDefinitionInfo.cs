namespace Skyline.DataMiner.MediaOps.Live.DOM.Interfaces
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Sections;

	/// <summary>
	/// Interface for DOM definition information.
	/// </summary>
	public interface IDomDefinitionInfo
	{
		/// <summary>
		/// Gets the DOM definition.
		/// </summary>
		DomDefinition Definition { get; }

		/// <summary>
		/// Gets the section definitions associated with this DOM definition.
		/// </summary>
		IEnumerable<CustomSectionDefinition> SectionDefinitions { get; }
	}
}
