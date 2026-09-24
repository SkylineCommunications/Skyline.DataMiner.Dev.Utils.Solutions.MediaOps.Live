namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	/// <summary>
	/// The discriminators that identify the concrete type of an orchestration input item when it is serialized.
	/// </summary>
	public static class OrchestrationInputKind
	{
		/// <summary>
		/// A group that bundles other input items.
		/// </summary>
		public const string Group = "group";

		/// <summary>
		/// A free text input field.
		/// </summary>
		public const string Text = "text";

		/// <summary>
		/// A numeric input field.
		/// </summary>
		public const string Number = "number";

		/// <summary>
		/// An input field that is limited to a predefined set of options.
		/// </summary>
		public const string Discrete = "discrete";

		/// <summary>
		/// A date and time input field.
		/// </summary>
		public const string DateTime = "dateTime";

		/// <summary>
		/// A duration input field.
		/// </summary>
		public const string TimeSpan = "timeSpan";

		/// <summary>
		/// A field that is backed by a profile parameter and whose definition is only known once it is resolved.
		/// </summary>
		public const string Profile = "profile";
	}
}
