namespace Skyline.DataMiner.MediaOps.Live.DOM.Tools
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.DOM.Interfaces;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;

	public static class DomModuleInstaller
	{
		public static void Install(Func<DMSMessage[], DMSMessage[]> messageHandler, IDomModuleInfo domModuleInfo, Action<string> logAction)
		{
			if (messageHandler == null)
			{
				throw new ArgumentNullException(nameof(messageHandler));
			}

			if (domModuleInfo == null)
			{
				throw new ArgumentNullException(nameof(domModuleInfo));
			}

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
			CreateOrUpdate(
				helper.ModuleSettings,
				ModuleSettingsExposers.ModuleId.Equal(settings.ModuleId),
				settings,
				logAction);
		}

		private static void CreateOrUpdateDomDefinition(DomHelper helper, DomDefinition definition, Action<string> logAction)
		{
			CreateOrUpdate(
				helper.DomDefinitions,
				DomDefinitionExposers.Id.Equal(definition.ID),
				definition,
				logAction,
				MergeDomDefinitions);
		}

		private static void CreateOrUpdateSectionDefinition(DomHelper helper, CustomSectionDefinition definition, Action<string> logAction)
		{
			CreateOrUpdate(
				helper.SectionDefinitions,
				SectionDefinitionExposers.ID.Equal(definition.GetID()),
				definition,
				logAction,
				MergeSectionDefinitions);
		}

		private static void CreateOrUpdate<T, TNew>(ICrudHelperComponent<T> crudHelperComponent, FilterElement<T> filter, TNew newItem, Action<string> logAction, Action<TNew, T> mergeExistingAction = null)
			where T : DataType
			where TNew : T
		{
			Log(logAction, "Searching for", newItem);
			T existingItem = crudHelperComponent.Read(filter).SingleOrDefault();

			if (existingItem == null)
			{
				Log(logAction, "Creating", newItem);
				crudHelperComponent.Create(newItem);
			}
			else
			{
				if (existingItem.Equals(newItem))
				{
					Log(logAction, "Skipping", newItem);
					return;
				}

				Log(logAction, "Updating", newItem);
				mergeExistingAction?.Invoke(newItem, existingItem);
				crudHelperComponent.Update(newItem);
			}
		}

		private static void MergeDomDefinitions(DomDefinition newDefinition, DomDefinition existing)
		{
			var newSectionDefinitionLinks = newDefinition.SectionDefinitionLinks
				.Select(x => x.SectionDefinitionID)
				.ToList();

			foreach (var existingSectionDefinitionLink in existing.SectionDefinitionLinks)
			{
				if (!newSectionDefinitionLinks.Contains(existingSectionDefinitionLink.SectionDefinitionID))
				{
					existingSectionDefinitionLink.IsSoftDeleted = true;
					newDefinition.SectionDefinitionLinks.Add(existingSectionDefinitionLink);
				}
			}
		}

		private static void MergeSectionDefinitions(CustomSectionDefinition newDefinition, SectionDefinition existing)
		{
			var newFieldDescriptors = newDefinition.GetAllFieldDescriptors()
				.Select(x => x.ID)
				.ToList();

			foreach (var existingFieldDescriptor in existing.GetAllFieldDescriptors())
			{
				if (!newFieldDescriptors.Contains(existingFieldDescriptor.ID))
				{
					existingFieldDescriptor.IsSoftDeleted = true;
					newDefinition.AddOrReplaceFieldDescriptor(existingFieldDescriptor);
				}
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
