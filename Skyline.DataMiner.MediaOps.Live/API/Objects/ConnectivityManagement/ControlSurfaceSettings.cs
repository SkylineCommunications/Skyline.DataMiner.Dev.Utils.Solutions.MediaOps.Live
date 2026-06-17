namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	/// <summary>
	/// Settings used by the Control Surface.
	/// </summary>
	public class ControlSurfaceSettings : ApiObject<ControlSurfaceSettings>
	{
		/// <summary>
		/// The placeholder that is replaced by the job reference when resolving the job details URL.
		/// </summary>
		public const string JobReferencePlaceholder = "[JOBREFERENCE]";

		private readonly ControlSurfaceSettingsInstance _domInstance;

		/// <summary>
		/// Initializes a new instance of the <see cref="ControlSurfaceSettings"/> class.
		/// </summary>
		public ControlSurfaceSettings() : this(new ControlSurfaceSettingsInstance())
		{
		}

		internal ControlSurfaceSettings(ControlSurfaceSettingsInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal ControlSurfaceSettings(DomInstance domInstance) : this(new ControlSurfaceSettingsInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcConnectivityManagementIds.Definitions.ControlSurfaceSettings;

		/// <summary>
		/// Gets or sets a value indicating whether the job details link is enabled in the Control Surface.
		/// </summary>
		public bool JobDetailsLinkEnabled
		{
			get
			{
				return _domInstance.ControlSurfaceSettings.JobDetailsLinkEnabled ?? false;
			}

			set
			{
				_domInstance.ControlSurfaceSettings.JobDetailsLinkEnabled = value;
			}
		}

		/// <summary>
		/// Gets or sets the URL template used to build the job details link.
		/// </summary>
		/// <remarks>
		/// The <see cref="JobReferencePlaceholder"/> placeholder is replaced by the job reference when resolving the URL.
		/// </remarks>
		public string JobDetailsUrlTemplate
		{
			get
			{
				return _domInstance.ControlSurfaceSettings.JobDetailsUrlTemplate;
			}

			set
			{
				_domInstance.ControlSurfaceSettings.JobDetailsUrlTemplate = value;
			}
		}

		/// <summary>
		/// Resolves the job details URL for the specified job reference using the configured template.
		/// </summary>
		/// <param name="jobReference">The job reference to insert into the template.</param>
		/// <returns>The resolved URL, or <see langword="null"/> if the link is disabled, no template is configured, or no job reference is provided.</returns>
		public string ResolveJobDetailsUrl(string jobReference)
		{
			if (!JobDetailsLinkEnabled
				|| String.IsNullOrWhiteSpace(JobDetailsUrlTemplate)
				|| String.IsNullOrWhiteSpace(jobReference))
			{
				return null;
			}

			return JobDetailsUrlTemplate.Replace(JobReferencePlaceholder, jobReference);
		}
	}
}
