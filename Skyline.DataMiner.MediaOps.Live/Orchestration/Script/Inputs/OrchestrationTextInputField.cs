namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;

	/// <summary>
	/// An orchestration input field that accepts free text.
	/// </summary>
	public class OrchestrationTextInputField : OrchestrationInputField
	{
		/// <inheritdoc/>
		public override string Kind => OrchestrationInputKind.Text;

		/// <inheritdoc/>
		public override bool IsValidValue(object value, out string error)
		{
			if (!base.IsValidValue(value, out error))
			{
				return false;
			}

			if (IsRequired && String.IsNullOrWhiteSpace(OrchestrationInputValueConverter.ToStringValue(value)))
			{
				error = $"'{Name}' requires a value.";
				return false;
			}

			return true;
		}
	}
}
