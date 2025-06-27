namespace Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcConnectivityManagement
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;

	internal class ConnectionDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Connection")
		{
			ID = SlcConnectivityManagementIds.Definitions.Connection,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcConnectivityManagementIds.Sections.ConnectionInfo.Id),
			},
		};

		public IEnumerable<SectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetConnectionInfoSectionDefinition(),
		};

		private static SectionDefinition GetConnectionInfoSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcConnectivityManagementIds.Sections.ConnectionInfo.Id,
				Name = "Connection Info",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcConnectivityManagementIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcConnectivityManagementIds.Sections.ConnectionInfo.Destination,
					Name = "Destination",
					IsOptional = false,
					DomDefinitionIds = { SlcConnectivityManagementIds.Definitions.Endpoint },
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(bool),
					ID = SlcConnectivityManagementIds.Sections.ConnectionInfo.IsConnected,
					Name = "Is Connected",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcConnectivityManagementIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcConnectivityManagementIds.Sections.ConnectionInfo.ConnectedSource,
					Name = "Connected Source",
					IsOptional = true,
					DomDefinitionIds = { SlcConnectivityManagementIds.Definitions.Endpoint },
				});

			return sectionDefinition;
		}
	}
}
