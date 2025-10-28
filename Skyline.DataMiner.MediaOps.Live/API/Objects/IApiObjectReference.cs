namespace Skyline.DataMiner.MediaOps.Live.API.Objects
{
	using System;

	/// <summary>
	/// Interface for API object references.
	/// </summary>
	public interface IApiObjectReference
	{
		/// <summary>
		/// Gets the unique identifier of the API object.
		/// </summary>
		Guid ID { get; }
	}
}
