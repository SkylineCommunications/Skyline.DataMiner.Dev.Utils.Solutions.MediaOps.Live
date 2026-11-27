namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration
{
	using System;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;

	/// <summary>
	/// A single key/value pair of the job context, owned by one node of the job.
	/// </summary>
	internal class OrchestrationNodeContextValue : ApiObject<OrchestrationNodeContextValue>
	{
		private readonly OrchestrationNodeContextValueInstance _domInstance;

		public OrchestrationNodeContextValue() : this(new OrchestrationNodeContextValueInstance())
		{
		}

		internal OrchestrationNodeContextValue(OrchestrationNodeContextValueInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal OrchestrationNodeContextValue(DomInstance domInstance) : this(new OrchestrationNodeContextValueInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcOrchestrationIds.Definitions.OrchestrationNodeContextValue;

		public ApiObjectReference<OrchestrationJobInfo> JobInfoReference
		{
			get
			{
				return new ApiObjectReference<OrchestrationJobInfo>(_domInstance.ContextValue.JobInformation ?? Guid.Empty);
			}

			set
			{
				_domInstance.ContextValue.JobInformation = value == ApiObjectReference<OrchestrationJobInfo>.Empty ? null : value.ID;
			}
		}

		public string NodeId
		{
			get
			{
				return _domInstance.ContextValue.NodeID;
			}

			set
			{
				_domInstance.ContextValue.NodeID = value;
			}
		}

		public string Key
		{
			get
			{
				return _domInstance.ContextValue.Key;
			}

			set
			{
				_domInstance.ContextValue.Key = value;
			}
		}

		public string Value
		{
			get
			{
				return _domInstance.ContextValue.Value;
			}

			set
			{
				_domInstance.ContextValue.Value = value;
			}
		}
	}
}
