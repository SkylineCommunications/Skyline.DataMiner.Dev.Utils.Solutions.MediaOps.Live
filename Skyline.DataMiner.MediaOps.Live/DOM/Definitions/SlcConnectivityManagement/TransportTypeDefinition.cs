namespace Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcConnectivityManagement
{
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

		public IEnumerable<SectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetTransportTypeInfoSectionDefinition(),
		};

		private static SectionDefinition GetTransportTypeInfoSectionDefinition()
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
	}
}
