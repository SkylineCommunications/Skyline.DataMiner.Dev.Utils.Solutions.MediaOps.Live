namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.Sections
{
	using System;

	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	using NumericParameterDisplayInfo = Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes.NumericParameterDisplayInfo;

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

			var decimals = info.Decimals >= 0 && info.Decimals != Int32.MaxValue ? info.Decimals : 0;
			Value.Decimals = decimals;

			// Without an explicit step size, the number of decimals determines the smallest allowed increment.
			Value.StepSize = !Double.IsNaN(info.Step) && info.Step > 0 ? info.Step : Math.Pow(10, -decimals);

			if (!Double.IsNaN(info.Min) && info.Min != Double.MinValue)
			{
				Value.Minimum = info.Min;
			}

			if (!Double.IsNaN(info.Max) && info.Max != Double.MaxValue)
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