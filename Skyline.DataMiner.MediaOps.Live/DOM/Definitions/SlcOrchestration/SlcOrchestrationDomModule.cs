namespace Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Definitions.SlcOrchestration
{
	using System.Collections.Generic;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Settings;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;

	public class SlcOrchestrationDomModule : IDomModuleInfo
	{
		public ModuleSettings ModuleSettings { get; } = new ModuleSettings(SlcOrchestrationIds.ModuleId)
		{
			DomManagerSettings = new DomManagerSettings
			{
				DomInstanceHistorySettings = new DomInstanceHistorySettings
				{
					StorageBehavior = DomInstanceHistoryStorageBehavior.Disabled,
				},
				ScriptSettings = new ExecuteScriptOnDomInstanceActionSettings
				{
					ScriptType = OnDomInstanceActionScriptType.FullCrudMeta,
				},
			},
		};

		public string ModuleId => ModuleSettings.ModuleId;

		public IEnumerable<IDomDefinitionInfo> Definitions { get; } =
		[
			new OrchestrationEventDefinition(),
			new ConfigurationDefinition(),
			new OrchestrationJobInfoDefinition(),
		];
	}
}