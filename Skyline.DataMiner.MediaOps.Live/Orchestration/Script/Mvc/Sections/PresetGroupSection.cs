namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.Sections
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.DisplayTypes;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	internal class PresetGroupSection : ParameterGroupSection
	{
		public PresetGroupSection(PresetGroupDisplayInfo info) : base(info)
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}

			// Init widgets
			Value = new DropDown<PresetGroupDisplayInfo.PresetInfo>(info.Presets);
			Value.Changed += OnPreselectChanged;
		}

		public DropDown<PresetGroupDisplayInfo.PresetInfo> Value { get; }

		protected override void DefineHeaderLayout(int row)
		{
			base.DefineHeaderLayout(row);
			AddWidget(Value, row, 1);
		}

		private void OnPreselectChanged(object sender, DropDown<PresetGroupDisplayInfo.PresetInfo>.DropDownChangedEventArgs e)
		{
			if (e.Selected is null)
			{
				return;
			}

			foreach (var (info, parameterValue) in e.Selected.ParameterValues)
			{
				if (!ParameterSections.TryGetValue(info, out var section))
				{
					// No section found for the parameter. Probably section was added, because it already had a value, fine to skip it.
					continue;
				}

				section.SetValue(parameterValue);
			}
		}
	}
}