namespace Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcOrchestration
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;

	internal class ConfigurationDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Configuration")
		{
			ID = SlcOrchestrationIds.Definitions.Configuration,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcOrchestrationIds.Sections.NodeConfiguration.Id)
				{
					AllowMultipleSections = true,
					IsOptional = true,
				},
				new SectionDefinitionLink(SlcOrchestrationIds.Sections.Connection.Id)
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
						new FieldValueConcatenationItem(SlcOrchestrationIds.Sections.NodeConfiguration.NodeID),
					},
				},
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetNodeConfigurationSectionDefinition(),
			GetConnectionSectionDefinition(),
		};

		private static CustomSectionDefinition GetNodeConfigurationSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcOrchestrationIds.Sections.NodeConfiguration.Id,
				Name = "Node Configuration",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.NodeConfiguration.NodeID,
					Name = "Node ID",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.NodeConfiguration.NodeLabel,
					Name = "Node Label",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.NodeConfiguration.OrchestrationScriptName,
					Name = "Orchestration Script Name",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.NodeConfiguration.OrchestrationScriptArguments,
					Name = "Orchestration Script Arguments",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.NodeConfiguration.OrchestrationProfile,
					Name = "Orchestration Profile",
					IsOptional = true,
				});

			return sectionDefinition;
		}

		private static CustomSectionDefinition GetConnectionSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcOrchestrationIds.Sections.Connection.Id,
				Name = "Connection",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.Connection.DestinationNodeID,
					Name = "Destination Node ID",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.Connection.SourceNodeID,
					Name = "Source Node ID",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcOrchestrationIds.Sections.Connection.LevelMapping,
					Name = "Level Mapping",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcOrchestrationIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcOrchestrationIds.Sections.Connection.DestinationVSG,
					Name = "Destination VSG",
					IsOptional = false,
					DomDefinitionIds = { SlcConnectivityManagementIds.Definitions.VirtualSignalGroup },
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcOrchestrationIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcOrchestrationIds.Sections.Connection.SourceVSG,
					Name = "Source VSG",
					IsOptional = false,
					DomDefinitionIds = { SlcConnectivityManagementIds.Definitions.VirtualSignalGroup },
				});

			return sectionDefinition;
		}
	}
}
