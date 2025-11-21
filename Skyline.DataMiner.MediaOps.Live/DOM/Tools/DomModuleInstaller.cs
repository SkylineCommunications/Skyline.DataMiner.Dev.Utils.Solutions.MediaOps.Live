namespace Skyline.DataMiner.MediaOps.Live.DOM.Tools
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;

	public static class DomModuleInstaller
	{
		public static void Install(Func<DMSMessage[], DMSMessage[]> messageHandler, IDomModuleInfo domModuleInfo, Action<string> logAction)
		{
			if (messageHandler == null)
				throw new ArgumentNullException(nameof(messageHandler));
			if (domModuleInfo == null)
				throw new ArgumentNullException(nameof(domModuleInfo));

			var moduleSettingsHelper = new ModuleSettingsHelper(messageHandler);
			var domHelper = new DomHelper(messageHandler, domModuleInfo.ModuleId);

			CreateOrUpdateModuleSettings(moduleSettingsHelper, domModuleInfo.ModuleSettings, logAction);

			foreach (var domDefinitionInfo in domModuleInfo.Definitions)
			{
				foreach (var sectionDefinition in domDefinitionInfo.SectionDefinitions)
				{
					CreateOrUpdateSectionDefinition(domHelper, sectionDefinition, logAction);
				}

				CreateOrUpdateDomDefinition(domHelper, domDefinitionInfo.Definition, logAction);
			}
		}

		private static void CreateOrUpdateModuleSettings(ModuleSettingsHelper helper, ModuleSettings settings, Action<string> logAction)
		{
			if (settings is null)
				throw new ArgumentNullException(nameof(settings));

			var existing = helper.ModuleSettings.Read(ModuleSettingsExposers.ModuleId.Equal(settings.ModuleId)).SingleOrDefault();

			if (existing == null)
			{
				Log(logAction, "Creating", settings);
				helper.ModuleSettings.Create(settings);
			}
			else
			{
				if (settings.Equals(existing))
				{
					Log(logAction, "Skipping", settings);
					return;
				}

				Log(logAction, "Updating", settings);
				helper.ModuleSettings.Update(settings);
			}
		}

		private static void CreateOrUpdateDomDefinition(DomHelper helper, DomDefinition definition, Action<string> logAction)
		{
			if (definition is null)
				throw new ArgumentNullException(nameof(definition));

			var existing = helper.DomDefinitions.Read(DomDefinitionExposers.Id.Equal(definition.ID)).SingleOrDefault();

			if (existing == null)
			{
				Log(logAction, "Creating", definition);
				helper.DomDefinitions.Create(definition);
			}
			else
			{
				if (definition.Equals(existing))
				{
					Log(logAction, "Skipping", definition);
					return;
				}

				Log(logAction, "Updating", definition);
				MarkExistingSectionDefinitionLinksAsDeleted(definition, existing);
				helper.DomDefinitions.Update(definition);
			}
		}

		private static void CreateOrUpdateSectionDefinition(DomHelper helper, CustomSectionDefinition definition, Action<string> logAction)
		{
			if (definition is null)
				throw new ArgumentNullException(nameof(definition));

			var existing = helper.SectionDefinitions.Read(SectionDefinitionExposers.ID.Equal(definition.GetID())).SingleOrDefault();

			if (existing == null)
			{
				Log(logAction, "Creating", definition);
				helper.SectionDefinitions.Create(definition);
			}
			else
			{
				if (CompareSectionDefinitions(definition, existing))
				{
					Log(logAction, "Skipping", definition);
					return;
				}

				Log(logAction, "Updating", definition);
				MarkExistingFieldDescriptorsAsDeleted(definition, existing);
				helper.SectionDefinitions.Update(definition);
			}
		}

		private static bool CompareSectionDefinitions(SectionDefinition definition, SectionDefinition other)
		{
			if (definition is CustomSectionDefinition customDefinition &&
				other is CustomSectionDefinition customOther)
			{
				return customDefinition.Equals(customOther);
			}

			return definition.Equals(other);
		}

		private static void MarkExistingSectionDefinitionLinksAsDeleted(DomDefinition newDefinition, DomDefinition existing)
		{
			if (newDefinition == null)
				throw new ArgumentNullException(nameof(newDefinition));
			if (existing == null)
				throw new ArgumentNullException(nameof(existing));

			var newIds = newDefinition.SectionDefinitionLinks
				.Select(x => x.SectionDefinitionID)
				.ToHashSet();

			var deletedLinks = existing.SectionDefinitionLinks
				.Where(existingLink => !newIds.Contains(existingLink.SectionDefinitionID))
				.Select(existingLink =>
				{
					existingLink.IsSoftDeleted = true;
					return existingLink;
				});

			newDefinition.SectionDefinitionLinks.AddRange(deletedLinks);
		}

		private static void MarkExistingFieldDescriptorsAsDeleted(CustomSectionDefinition newDefinition, SectionDefinition existing)
		{
			if (newDefinition == null)
				throw new ArgumentNullException(nameof(newDefinition));
			if (existing == null)
				throw new ArgumentNullException(nameof(existing));

			var newIds = newDefinition.GetAllFieldDescriptors()
				.Select(x => x.ID)
				.ToHashSet();

			var deletedFieldDescriptors = existing.GetAllFieldDescriptors()
				.Where(fd => !newIds.Contains(fd.ID))
				.Select(fd =>
				{
					fd.IsSoftDeleted = true;
					return fd;
				});

			foreach (var deletedFieldDescriptor in deletedFieldDescriptors)
			{
				newDefinition.AddOrReplaceFieldDescriptor(deletedFieldDescriptor);
			}
		}

		private static void Log(Action<string> logAction, string action, DataType dataType)
		{
			logAction?.Invoke($"{action} {dataType.GetType().Name}: {GetName(dataType)} [{dataType.DataTypeID}].");
		}

		private static string GetName(DataType dataType)
		{
			switch (dataType)
			{
				case DomBehaviorDefinition domBehaviorDefinition:
					return domBehaviorDefinition.Name;

				case DomDefinition domDefinition:
					return domDefinition.Name;

				case DomInstance domInstance:
					return domInstance.Name;

				case DomTemplate domTemplate:
					return domTemplate.Name;

				case ModuleSettings moduleSettings:
					return moduleSettings.ModuleId;

				case CustomSectionDefinition customSectionDefinition:
					return customSectionDefinition.Name;

				default:
					return String.Empty;
			}
		}
	}
}
