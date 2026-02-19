namespace Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Definitions.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Tools;

	internal class OrchestrationEventDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Orchestration Event")
		{
			ID = SlcOrchestrationIds.Definitions.OrchestrationEvent,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcOrchestrationIds.Sections.OrchestrationEventInfo.Id),
				new SectionDefinitionLink(SlcOrchestrationIds.Sections.GlobalConfiguration.Id)
				{
					IsOptional = true,
				},
				new SectionDefinitionLink(SlcOrchestrationIds.Sections.ConfigurationInfo.Id)
				{
					IsOptional = true,
				},
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition
				{
					ConcatenationItems =
					{
						new FieldValueConcatenationItem(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventName),
					},
				},
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetOrchestrationEventInfoSectionDefinition(),
			GetGlobalConfigurationSectionDefinition(),
			GetConfigurationInfoSectionDefinition(),
		};

		private static CustomSectionDefinition GetOrchestrationEventInfoSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcOrchestrationIds.Sections.OrchestrationEventInfo.Id,
				Name = "Orchestration Event Info",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventName,
					Name = "Event Name",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new GenericEnumFieldDescriptor
				{
					FieldType = typeof(GenericEnum<int>),
					ID = SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventType,
					Name = "Event Type",
					IsOptional = false,
					GenericEnumInstance = GenericEnumFactory.Create<SlcOrchestrationIds.Enums.EventType>(),
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new GenericEnumFieldDescriptor
				{
					FieldType = typeof(GenericEnum<int>),
					ID = SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventState,
					Name = "Event State",
					IsOptional = false,
					GenericEnumInstance = GenericEnumFactory.Create<SlcOrchestrationIds.Enums.EventState>(),
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(DateTime),
					ID = SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime,
					Name = "Event Time",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcOrchestrationIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobInformation,
					Name = "Job Information",
					IsOptional = false,
					DomDefinitionIds = { SlcOrchestrationIds.Definitions.OrchestrationJobInfo },
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.OrchestrationEventInfo.FailureInfo,
					Name = "Failure Info",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.OrchestrationEventInfo.SchedulerReference,
					Name = "Scheduler Reference",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(DateTime),
					ID = SlcOrchestrationIds.Sections.OrchestrationEventInfo.ActualStartTime,
					Name = "Actual Start Time",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(TimeSpan),
					ID = SlcOrchestrationIds.Sections.OrchestrationEventInfo.OrchestrationDuration,
					Name = "Orchestration Duration",
					IsOptional = true,
				});

			return sectionDefinition;
		}

		private static CustomSectionDefinition GetGlobalConfigurationSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcOrchestrationIds.Sections.GlobalConfiguration.Id,
				Name = "Global Configuration",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.GlobalConfiguration.OrchestrationScriptName,
					Name = "Orchestration Script Name",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.GlobalConfiguration.OrchestrationScriptArguments,
					Name = "Orchestration Script Arguments",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.GlobalConfiguration.OrchestrationProfile,
					Name = "Orchestration Profile",
					IsOptional = true,
				});

			return sectionDefinition;
		}

		private static CustomSectionDefinition GetConfigurationInfoSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcOrchestrationIds.Sections.ConfigurationInfo.Id,
				Name = "Configuration Info",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcOrchestrationIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcOrchestrationIds.Sections.ConfigurationInfo.Configuration,
					Name = "Configuration",
					IsOptional = false,
					DomDefinitionIds = { SlcOrchestrationIds.Definitions.Configuration },
				});

			return sectionDefinition;
		}
	}
}
