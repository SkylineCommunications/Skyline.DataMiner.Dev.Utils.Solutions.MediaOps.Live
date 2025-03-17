namespace Skyline.DataMiner.MediaOps.Live.DOM.Interfaces
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.Modules;

	public interface IDomModuleInfo
	{
		string ModuleId { get; }

		ModuleSettings ModuleSettings { get; }

		IEnumerable<IDomDefinitionInfo> Definitions { get; }
	}
}
