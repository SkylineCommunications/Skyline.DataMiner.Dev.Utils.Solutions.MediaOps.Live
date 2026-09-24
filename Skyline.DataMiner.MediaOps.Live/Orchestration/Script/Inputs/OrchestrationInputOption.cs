namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;

	using Newtonsoft.Json;

	/// <summary>
	/// A single option of an <see cref="OrchestrationDiscreteInputField"/>.
	/// The display text is what the operator sees; the value is what the orchestration script receives.
	/// </summary>
	public class OrchestrationInputOption
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationInputOption"/> class.
		/// </summary>
		public OrchestrationInputOption()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationInputOption"/> class whose display text equals its value.
		/// </summary>
		/// <param name="value">The value that is passed to the orchestration script.</param>
		public OrchestrationInputOption(string value) : this(value, value)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationInputOption"/> class.
		/// </summary>
		/// <param name="display">The text that is shown to the operator.</param>
		/// <param name="value">The value that is passed to the orchestration script.</param>
		public OrchestrationInputOption(string display, OrchestrationInputValue value)
		{
			Display = display;
			Value = value;
		}

		/// <summary>
		/// Gets or sets the text that is shown to the operator.
		/// </summary>
		[JsonProperty("display")]
		public string Display { get; set; }

		/// <summary>
		/// Gets the value that is passed to the orchestration script.
		/// </summary>
		[JsonProperty("value")]
		public OrchestrationInputValue Value { get; private set; }

		/// <summary>
		/// Gets the display text, falling back to the value when no display text was provided.
		/// </summary>
		/// <returns>The text to show to the operator.</returns>
		public string GetDisplayText()
		{
			return String.IsNullOrEmpty(Display) ? Value?.ToString() : Display;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return GetDisplayText();
		}
	}
}
