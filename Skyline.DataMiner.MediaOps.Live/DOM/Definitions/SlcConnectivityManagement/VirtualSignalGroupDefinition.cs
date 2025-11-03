namespace Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcConnectivityManagement
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Utils.Categories.DOM.Definitions;

	internal class VirtualSignalGroupDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Virtual Signal Group")
		{
			ID = SlcConnectivityManagementIds.Definitions.VirtualSignalGroup,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Id),
				new SectionDefinitionLink(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevel.Id) { AllowMultipleSections = true, IsOptional = true },
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition
				{
					ConcatenationItems =
					{
						new FieldValueConcatenationItem(SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Name),
					},
				},
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetVirtualSignalGroupInfoSectionDefinition(),
			GetLevelsInfoSectionDefinition(),
		};

		private static CustomSectionDefinition GetVirtualSignalGroupInfoSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Id,
				Name = "Virtual Signal Group Info",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Name,
					Name = "Name",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Description,
					Name = "Description",
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new GenericEnumFieldDescriptor
				{
					FieldType = typeof(GenericEnum<int>),
					ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Role,
					Name = "Role",
					IsOptional = false,
					GenericEnumInstance = GenericEnumFactory.Create<SlcConnectivityManagementIds.Enums.Role>(),
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcConnectivityManagementIds.ModuleId)
				{
					FieldType = typeof(List<Guid>),
					ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupInfo.Categories,
					Name = "Categories",
				});

			return sectionDefinition;
		}

		private static CustomSectionDefinition GetLevelsInfoSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevel.Id,
				Name = "Virtual Signal Group Level",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcConnectivityManagementIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevel.Endpoint,
					Name = "Endpoint",
					IsOptional = false,
					DomDefinitionIds = { SlcConnectivityManagementIds.Definitions.Endpoint },
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcConnectivityManagementIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupLevel.Level,
					Name = "Level",
					IsOptional = false,
					DomDefinitionIds = { SlcConnectivityManagementIds.Definitions.Level },
				});

			return sectionDefinition;
		}
	}
}
