namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;

	/// <summary>
	/// Represents job information to store on the state of a single virtual signal group.
	/// </summary>
	/// <remarks>
	/// The job information is persisted independently of the lock state, so it survives a manual unlock
	/// and is only cleared explicitly (typically at the start of the post-roll).
	/// </remarks>
	public class VirtualSignalGroupJobInfoRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualSignalGroupJobInfoRequest"/> class.
		/// </summary>
		/// <param name="virtualSignalGroup">The virtual signal group to store the job information on.</param>
		/// <param name="jobReference">The reference of the related job.</param>
		/// <param name="jobName">The name of the related job.</param>
		/// <param name="jobDescription">The description of the related job.</param>
		public VirtualSignalGroupJobInfoRequest(
			VirtualSignalGroup virtualSignalGroup,
			string jobReference,
			string jobName,
			string jobDescription)
		{
			VirtualSignalGroup = virtualSignalGroup ?? throw new ArgumentNullException(nameof(virtualSignalGroup));
			JobReference = jobReference;
			JobName = jobName;
			JobDescription = jobDescription;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualSignalGroupJobInfoRequest"/> class for clearing job information.
		/// </summary>
		/// <param name="virtualSignalGroup">The virtual signal group to clear the job information from.</param>
		public VirtualSignalGroupJobInfoRequest(VirtualSignalGroup virtualSignalGroup)
		{
			VirtualSignalGroup = virtualSignalGroup ?? throw new ArgumentNullException(nameof(virtualSignalGroup));
			JobReference = null;
			JobName = null;
			JobDescription = null;
		}

		/// <summary>
		/// Gets the virtual signal group to store the job information on.
		/// </summary>
		public VirtualSignalGroup VirtualSignalGroup { get; }

		/// <summary>
		/// Gets the reference of the related job.
		/// </summary>
		public string JobReference { get; }

		/// <summary>
		/// Gets the name of the related job.
		/// </summary>
		public string JobName { get; }

		/// <summary>
		/// Gets the description of the related job.
		/// </summary>
		public string JobDescription { get; }

		/// <summary>
		/// Gets a value indicating whether this request contains any job information.
		/// </summary>
		public bool HasJobInfo =>
			!String.IsNullOrWhiteSpace(JobReference)
			|| !String.IsNullOrWhiteSpace(JobName)
			|| !String.IsNullOrWhiteSpace(JobDescription);
	}
}
