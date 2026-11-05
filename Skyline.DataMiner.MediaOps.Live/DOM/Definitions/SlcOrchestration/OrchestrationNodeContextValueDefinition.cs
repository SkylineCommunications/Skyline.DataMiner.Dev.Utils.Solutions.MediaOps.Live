namespace Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Definitions.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;

	// One instance per job, node and key, so overlapping events of the same node only contend on the same key.
	internal class OrchestrationNodeContextValueDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Orchestration Node Context Value")
		{
			ID = SlcOrchestrationIds.Definitions.OrchestrationNodeContextValue,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcOrchestrationIds.Sections.ContextValue.Id)
				{
					AllowMultipleSections = false,
					IsOptional = false,
				},
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition
				{
					ConcatenationItems =
					{
						new FieldValueConcatenationItem(SlcOrchestrationIds.Sections.ContextValue.Key),
					},
				},
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetContextValueSectionDefinition(),
		};

		private static CustomSectionDefinition GetContextValueSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcOrchestrationIds.Sections.ContextValue.Id,
				Name = "Context Value",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcOrchestrationIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcOrchestrationIds.Sections.ContextValue.JobInformation,
					Name = "Job Information",
					IsOptional = false,
					DomDefinitionIds = { SlcOrchestrationIds.Definitions.OrchestrationJobInfo },
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.ContextValue.NodeID,
					Name = "Node ID",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.ContextValue.Key,
					Name = "Key",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.ContextValue.Value,
					Name = "Value",
					IsOptional = false,
				});

			return sectionDefinition;
		}
	}
}
