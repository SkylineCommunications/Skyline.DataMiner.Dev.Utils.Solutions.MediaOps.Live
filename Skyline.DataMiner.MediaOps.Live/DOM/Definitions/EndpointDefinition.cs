namespace Skyline.DataMiner.MediaOps.Live.DOM.Definitions
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

	internal class EndpointDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Endpoint")
		{
			ID = SlcConnectivityManagementIds.Definitions.Endpoint,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcConnectivityManagementIds.Sections.EndpointInfo.Id),
				new SectionDefinitionLink(SlcConnectivityManagementIds.Sections.TransportTypeTsoip.Id) { IsOptional = true },
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition
				{
					ConcatenationItems =
					{
						new FieldValueConcatenationItem(SlcConnectivityManagementIds.Sections.EndpointInfo.Name),
					},
				},
			},
		};

		public IEnumerable<SectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetEndpointInfoSectionDefinition(),
			GetTransportTypeTsoipSectionDefinition(),
		};

		private static SectionDefinition GetEndpointInfoSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcConnectivityManagementIds.Sections.EndpointInfo.Id,
				Name = "Endpoint Info",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.EndpointInfo.Name,
					Name = "Name",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new GenericEnumFieldDescriptor
				{
					FieldType = typeof(GenericEnum<int>),
					ID = SlcConnectivityManagementIds.Sections.EndpointInfo.Role,
					Name = "Role",
					IsOptional = false,
					GenericEnumInstance = GenericEnumFactory.Create<SlcConnectivityManagementIds.Enums.Role>(),
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new ElementFieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.EndpointInfo.Element,
					Name = "Element",
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.EndpointInfo.Identifier,
					Name = "Identifier",
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new ElementFieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.EndpointInfo.ControlElement,
					Name = "Control Element",
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.EndpointInfo.ControlIdentifier,
					Name = "Control Identifier",
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcConnectivityManagementIds.ModuleId)
				{
					FieldType = typeof(List<Guid>),
					ID = SlcConnectivityManagementIds.Sections.EndpointInfo.TransportMetadata,
					Name = "Transport Metadata",
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcConnectivityManagementIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcConnectivityManagementIds.Sections.EndpointInfo.TransportType,
					Name = "Transport Type",
					IsOptional = false,
					DomDefinitionIds = { SlcConnectivityManagementIds.Definitions.TransportType },
				});

			return sectionDefinition;
		}

		private static SectionDefinition GetTransportTypeTsoipSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcConnectivityManagementIds.Sections.TransportTypeTsoip.Id,
				Name = "Transport Type.TSoIP",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.TransportTypeTsoip.SourceIP,
					Name = "Source IP",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.TransportTypeTsoip.MulticastIP,
					Name = "Multicast IP",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(long),
					ID = SlcConnectivityManagementIds.Sections.TransportTypeTsoip.Port,
					Name = "Port",
					IsOptional = true,
				});

			return sectionDefinition;
		}
	}
}
