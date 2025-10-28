namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	/// <summary>
	/// Represents the input data for an orchestration script.
	/// </summary>
	public class OrchestrationScriptInput
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationScriptInput"/> class with default values.
		/// </summary>
		public OrchestrationScriptInput()
			: this(new Dictionary<string, object>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationScriptInput"/> class.
		/// </summary>
		/// <param name="profileParameterValues">The dictionary of profile parameter values.</param>
		public OrchestrationScriptInput(Dictionary<string, object> profileParameterValues) : this(profileParameterValues, String.Empty)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationScriptInput"/> class.
		/// </summary>
		/// <param name="profileParameterValues">The dictionary of profile parameter values.</param>
		/// <param name="profileInstance">The profile instance name.</param>
		[JsonConstructor]
		public OrchestrationScriptInput(Dictionary<string, object> profileParameterValues, string profileInstance)
		{
			ProfileParameterValues = profileParameterValues;
			ProfileInstance = profileInstance;
			Metadata = new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the dictionary of profile parameter values.
		/// </summary>
		public Dictionary<string, object> ProfileParameterValues { get; }

		/// <summary>
		/// Gets the profile instance name.
		/// </summary>
		public string ProfileInstance { get; }

		/// <summary>
		/// Gets the dictionary of metadata.
		/// </summary>
		public Dictionary<string, string> Metadata { get; }
	}
}