namespace Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcConnectivityManagement
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;

	internal class TransportTypeDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Transport Type")
		{
			ID = SlcConnectivityManagementIds.Definitions.TransportType,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcConnectivityManagementIds.Sections.TransportTypeInfo.Id),
				new SectionDefinitionLink(SlcConnectivityManagementIds.Sections.TransportTypeField.Id) { AllowMultipleSections = true, IsOptional = true },
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition
				{
					ConcatenationItems =
					{
						new FieldValueConcatenationItem(SlcConnectivityManagementIds.Sections.TransportTypeInfo.Name),
					},
				},
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetTransportTypeInfoSectionDefinition(),
			GetTransportTypeFieldSectionDefinition(),
		};

		private static CustomSectionDefinition GetTransportTypeInfoSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcConnectivityManagementIds.Sections.TransportTypeInfo.Id,
				Name = "Transport Type Info",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.TransportTypeInfo.Name,
					Name = "Name",
					IsOptional = false,
				});

			return sectionDefinition;
		}

		private static CustomSectionDefinition GetTransportTypeFieldSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcConnectivityManagementIds.Sections.TransportTypeField.Id,
				Name = "Transport Type Field",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.TransportTypeField.Name,
					Name = "Name",
					IsOptional = false,
				});

			return sectionDefinition;
		}
	}
}
