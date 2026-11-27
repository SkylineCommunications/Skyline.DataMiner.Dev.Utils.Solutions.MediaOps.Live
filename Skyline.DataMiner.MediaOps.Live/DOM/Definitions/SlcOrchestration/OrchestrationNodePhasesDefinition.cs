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

	// One instance per event and node, so overlapping events of the same node never write the same instance.
	internal class OrchestrationNodePhasesDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Orchestration Node Phases")
		{
			ID = SlcOrchestrationIds.Definitions.OrchestrationNodePhases,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcOrchestrationIds.Sections.NodePhasesInfo.Id)
				{
					AllowMultipleSections = false,
					IsOptional = false,
				},
				new SectionDefinitionLink(SlcOrchestrationIds.Sections.PhaseStatus.Id)
				{
					AllowMultipleSections = true,
					IsOptional = true,
				},
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition
				{
					ConcatenationItems =
					{
						new FieldValueConcatenationItem(SlcOrchestrationIds.Sections.NodePhasesInfo.NodeID),
					},
				},
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetNodePhasesInfoSectionDefinition(),
			GetPhaseStatusSectionDefinition(),
		};

		private static CustomSectionDefinition GetNodePhasesInfoSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcOrchestrationIds.Sections.NodePhasesInfo.Id,
				Name = "Node Phases Info",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcOrchestrationIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcOrchestrationIds.Sections.NodePhasesInfo.JobInformation,
					Name = "Job Information",
					IsOptional = false,
					DomDefinitionIds = { SlcOrchestrationIds.Definitions.OrchestrationJobInfo },
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcOrchestrationIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcOrchestrationIds.Sections.NodePhasesInfo.OrchestrationEvent,
					Name = "Orchestration Event",
					IsOptional = false,
					DomDefinitionIds = { SlcOrchestrationIds.Definitions.OrchestrationEvent },
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.NodePhasesInfo.NodeID,
					Name = "Node ID",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.NodePhasesInfo.PhaseLayoutFingerprint,
					Name = "Phase Layout Fingerprint",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.NodePhasesInfo.ResumeSchedulerReference,
					Name = "Resume Scheduler Reference",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(DateTime),
					ID = SlcOrchestrationIds.Sections.NodePhasesInfo.WaitStarted,
					Name = "Wait Started",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(long),
					ID = SlcOrchestrationIds.Sections.NodePhasesInfo.ResumePhaseIndex,
					Name = "Resume Phase Index",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(DateTime),
					ID = SlcOrchestrationIds.Sections.NodePhasesInfo.ResumeTime,
					Name = "Resume Time",
					IsOptional = true,
				});

			return sectionDefinition;
		}

		private static CustomSectionDefinition GetPhaseStatusSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcOrchestrationIds.Sections.PhaseStatus.Id,
				Name = "Phase Status",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.PhaseStatus.PhaseKey,
					Name = "Phase Key",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(long),
					ID = SlcOrchestrationIds.Sections.PhaseStatus.Order,
					Name = "Order",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new GenericEnumFieldDescriptor
				{
					FieldType = typeof(GenericEnum<int>),
					ID = SlcOrchestrationIds.Sections.PhaseStatus.State,
					Name = "State",
					IsOptional = false,
					GenericEnumInstance = GenericEnumFactory.Create<SlcOrchestrationIds.Enums.PhaseState>(),
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.PhaseStatus.Message,
					Name = "Message",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(DateTime),
					ID = SlcOrchestrationIds.Sections.PhaseStatus.LastUpdated,
					Name = "Last Updated",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(DateTime),
					ID = SlcOrchestrationIds.Sections.PhaseStatus.Started,
					Name = "Started",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(DateTime),
					ID = SlcOrchestrationIds.Sections.PhaseStatus.Ended,
					Name = "Ended",
					IsOptional = true,
				});

			return sectionDefinition;
		}
	}
}
