namespace Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcOrchestration
{
	using System.Collections.Generic;
	using Skyline.DataMiner.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;

	internal class OrchestrationJobInfoDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Orchestration Job Info")
		{
			ID = SlcOrchestrationIds.Definitions.OrchestrationJobInfo,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcOrchestrationIds.Sections.JobInfo.Id)
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
						new FieldValueConcatenationItem(SlcOrchestrationIds.Sections.JobInfo.JobReference),
					},
				},
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetJobInfoSectionDefinition(),
		};

		private static CustomSectionDefinition GetJobInfoSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcOrchestrationIds.Sections.JobInfo.Id,
				Name = "Job Info",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.JobInfo.JobReference,
					Name = "Job Reference",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.JobInfo.MonitoringService,
					Name = "Monitoring Service",
					IsOptional = true,
				});

			return sectionDefinition;
		}
	}
}
