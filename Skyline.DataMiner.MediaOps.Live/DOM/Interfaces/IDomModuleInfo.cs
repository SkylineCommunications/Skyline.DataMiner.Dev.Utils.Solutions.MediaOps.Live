namespace Skyline.DataMiner.MediaOps.Live.DOM.Interfaces
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.Modules;

	/// <summary>
	/// Provides information about a DOM (DataMiner Object Model) module.
	/// </summary>
	public interface IDomModuleInfo
	{
		/// <summary>
		/// Gets the unique identifier of the DOM module.
		/// </summary>
		string ModuleId { get; }

		/// <summary>
		/// Gets the module settings.
		/// </summary>
		ModuleSettings ModuleSettings { get; }

		/// <summary>
		/// Gets the collection of DOM definition information contained in this module.
		/// </summary>
		IEnumerable<IDomDefinitionInfo> Definitions { get; }
	}
}
