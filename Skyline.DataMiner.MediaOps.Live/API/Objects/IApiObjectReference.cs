namespace Skyline.DataMiner.MediaOps.Live.API.Objects
{
	using System;

	/// <summary>
	/// Represents a reference to an API object.
	/// </summary>
	public interface IApiObjectReference
	{
		/// <summary>
		/// Gets the unique identifier of the API object.
		/// </summary>
		Guid ID { get; }
	}
}
