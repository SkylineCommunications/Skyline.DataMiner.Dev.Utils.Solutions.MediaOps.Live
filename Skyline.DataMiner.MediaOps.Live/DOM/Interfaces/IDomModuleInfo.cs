namespace Skyline.DataMiner.MediaOps.Live.DOM.Interfaces
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.Modules;

	/// <summary>
	/// Interface for DOM module information.
	/// </summary>
	public interface IDomModuleInfo
	{
		/// <summary>
		/// Gets the module ID.
		/// </summary>
		string ModuleId { get; }

		/// <summary>
		/// Gets the module settings.
		/// </summary>
		ModuleSettings ModuleSettings { get; }

		/// <summary>
		/// Gets the definitions contained in this module.
		/// </summary>
		IEnumerable<IDomDefinitionInfo> Definitions { get; }
	}
}
