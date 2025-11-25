namespace Skyline.DataMiner.MediaOps.Live.DOM.Definitions.SlcConnectivityManagement
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;

	internal class VirtualSignalGroupStateDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Virtual Signal Group State")
		{
			ID = SlcConnectivityManagementIds.Definitions.VirtualSignalGroupState,
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcConnectivityManagementIds.Sections.VirtualSignalGroupStateInfo.Id),
				new SectionDefinitionLink(SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.Id) { IsOptional = true },
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetVirtualSignalGroupStateInfoSectionDefinition(),
			GetVirtualSignalGroupLockDefinition(),
		};

		private static CustomSectionDefinition GetVirtualSignalGroupStateInfoSectionDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupStateInfo.Id,
				Name = "Virtual Signal Group State Info",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new DomInstanceFieldDescriptor(SlcConnectivityManagementIds.ModuleId)
				{
					FieldType = typeof(Guid),
					ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupStateInfo.VirtualSignalGroupReference,
					Name = "Virtual Signal Group Reference",
					IsOptional = false,
					DomDefinitionIds = { SlcConnectivityManagementIds.Definitions.VirtualSignalGroup },
				});

			return sectionDefinition;
		}

		private static CustomSectionDefinition GetVirtualSignalGroupLockDefinition()
		{
			var sectionDefinition = new CustomSectionDefinition
			{
				ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.Id,
				Name = "Virtual Signal Group Lock",
			};

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new GenericEnumFieldDescriptor
				{
					FieldType = typeof(GenericEnum<int>),
					ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockState,
					Name = "Lock State",
					IsOptional = false,
					GenericEnumInstance = GenericEnumFactory.Create<SlcConnectivityManagementIds.Enums.LockState>(),
					DefaultValue = new ValueWrapper<int>((int)SlcConnectivityManagementIds.Enums.LockState.Unlocked),
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockReason,
					Name = "Lock Reason",
					IsOptional = true,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockedBy,
					Name = "Locked By",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(DateTime),
					ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockTime,
					Name = "Lock Time",
					IsOptional = false,
				});

			sectionDefinition.AddOrReplaceFieldDescriptor(
				new FieldDescriptor
				{
					FieldType = typeof(string),
					ID = SlcConnectivityManagementIds.Sections.VirtualSignalGroupLock.LockJobReference,
					Name = "Lock Job Reference",
					IsOptional = true,
				});

			return sectionDefinition;
		}
	}
}
