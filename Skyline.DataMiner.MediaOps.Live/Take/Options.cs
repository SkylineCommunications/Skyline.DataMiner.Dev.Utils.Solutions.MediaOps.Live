namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;

	public abstract class OptionsBase
	{
		/// <summary>
		/// Gets or sets a value indicating whether to wait for the operation to complete.
		/// </summary>
		public bool WaitForCompletion { get; set; }

		/// <summary>
		/// Gets or sets the connection monitor to use for waiting for completion.
		/// If null, the default connection monitor will be used.
		/// </summary>
		public ConnectionMonitor ConnectionMonitor { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to bypass lock validation.
		/// </summary>
		public bool BypassLockValidation { get; set; }
	}

	/// <summary>
	/// Represents additional options for take operations.
	/// </summary>
	public class TakeOptions : OptionsBase
	{
	}

	/// <summary>
	/// Represents additional options for disconnect operations.
	/// </summary>
	public class DisconnectOptions : OptionsBase
	{
	}
}