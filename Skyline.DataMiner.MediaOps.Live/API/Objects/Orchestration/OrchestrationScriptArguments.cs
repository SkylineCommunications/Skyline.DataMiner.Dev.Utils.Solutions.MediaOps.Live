namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	using Skyline.DataMiner.MediaOps.Live.API.Enums;

	/// <summary>
	/// An argument that can be applied to an orchestration script.
	/// </summary>
	public class OrchestrationScriptArgument
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationScriptArgument"/> class.
		/// </summary>
		/// <param name="type">Type of parameter.</param>
		/// <param name="name">Parameter name.</param>
		/// <param name="value">Parameter value.</param>
		public OrchestrationScriptArgument(OrchestrationScriptArgumentType type, string name, string value)
		{
			Type = type;
			Name = name;
			Value = value;
		}

		/// <summary>
		/// Gets or sets the parameter type.
		/// </summary>
		public OrchestrationScriptArgumentType Type { get; set; }

		/// <summary>
		/// Gets or sets the name of the parameter.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the value of the parameter.
		/// </summary>
		public string Value { get; set; }
	}
}
