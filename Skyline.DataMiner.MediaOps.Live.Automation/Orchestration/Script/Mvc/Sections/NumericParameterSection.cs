namespace Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.Sections
{
	using System;

	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	using NumericParameterDisplayInfo = Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes.NumericParameterDisplayInfo;

	internal class NumericParameterSection : ParameterSection
	{
		public Numeric Value { get; }

		public Label Unit { get; }

		public NumericParameterSection(NumericParameterDisplayInfo info) : base(info)
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}

			// Init widgets
			Value = new Numeric();

			Value.StepSize = !Double.IsNaN(info.Step) && info.Step > 0 ? info.Step : 3;

			if (!Double.IsNaN(info.Decimals) && info.Decimals >= 0)
			{
				Value.Decimals = info.Decimals;
			}

			if (!Double.IsNaN(info.Min) && info.Min != double.MinValue)
			{
				Value.Minimum = info.Min;
			}

			if (!Double.IsNaN(info.Max) && info.Max != double.MaxValue)
			{
				Value.Maximum = info.Max;
			}

			Unit = String.IsNullOrWhiteSpace(info.Unit) ? null : new Label(info.Unit);
		}

		protected override void DefineLayout()
		{
			base.DefineLayout();

			if (Unit is null)
			{
				AddWidget(Value, 0, 1);
			}
			else
			{
				var section = new Section();
				section.AddWidget(Value, 0, 0);
				section.AddWidget(Unit, 0, 1);
				AddSection(section, 0, 1);
			}
		}

		public override void SetValue(object value)
		{
			Value.Value = value is null ? 0 : (double)value;
		}

		public override object GetValue()
		{
			return Value.Value;
		}
	}
}