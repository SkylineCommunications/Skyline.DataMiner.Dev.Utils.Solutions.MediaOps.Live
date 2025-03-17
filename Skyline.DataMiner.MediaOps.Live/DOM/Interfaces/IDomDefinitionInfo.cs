namespace Skyline.DataMiner.MediaOps.Live.DOM.Interfaces
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Sections;

	public interface IDomDefinitionInfo
	{
		DomDefinition Definition { get; }

		IEnumerable<SectionDefinition> SectionDefinitions { get; }
	}
}
