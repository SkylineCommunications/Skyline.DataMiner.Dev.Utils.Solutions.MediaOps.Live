namespace Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.Sections
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	using IParameterGroupDisplayInfo = Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes.IParameterGroupDisplayInfo;

	public abstract class ParameterGroupSection : Section
	{
		public Label Label { get; }

		public IReadOnlyDictionary<ParameterInfo, ParameterSection> ParameterSections { get; private set; }

		protected ParameterGroupSection(IParameterGroupDisplayInfo displayInfo)
		{
			if (displayInfo == null)
			{
				throw new ArgumentNullException(nameof(displayInfo));
			}

			// Init widgets
			Label = new Label(displayInfo.Label);
		}

		public IEnumerable<(ParameterInfo, ParameterSection)> InitializeSection(IEnumerable<ParameterInfo> parameters)
		{
			var row = 0;
			var headerSection = DefineHeaderSection();
			AddSection(headerSection, row, 0);

			row += headerSection.RowCount;

			var parameterSections = new Dictionary<ParameterInfo, ParameterSection>();
			ParameterSections = parameterSections;

			foreach (var parameter in parameters)
			{
				var section = parameter.DisplayInfo.CreateParameterSection();
				section.InitializeSection();
				section.SetValue(parameter.Value);
				parameterSections.Add(parameter, section);
				AddSection(section, row++, 1);
				yield return (parameter, section);
			}
		}

		protected virtual Section DefineHeaderSection()
		{
			var section = new Section();
			section.AddWidget(Label, 0, 0);
			return section;
		}
	}
}