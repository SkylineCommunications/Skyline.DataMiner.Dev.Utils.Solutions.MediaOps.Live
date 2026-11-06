namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Tools;

	internal class NodeContextValueRepository : Repository<OrchestrationNodeContextValue>
	{
		internal const int MaxKeyLength = 128;
		internal const int MaxValueLength = 4096;
		internal const int MaxKeysPerNode = 50;

		internal NodeContextValueRepository(MediaOpsLiveApi api) : base(api, api.SlcOrchestrationHelper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => OrchestrationNodeContextValue.DomDefinition;

		internal IReadOnlyDictionary<string, string> GetValues(Guid jobInfoId, string nodeId)
		{
			return ReadDom(CreateNodeFilter(jobInfoId, nodeId))
				.GroupBy(x => x.Key, StringComparer.Ordinal)
				.ToDictionary(g => g.Key, g => SelectNewest(g).Value, StringComparer.Ordinal);
		}

		internal bool TryGetValue(Guid jobInfoId, string nodeId, string key, out string value)
		{
			OrchestrationNodeContextValue match = SelectNewest(ReadDom(CreateKeyFilter(jobInfoId, nodeId, key)));

			value = match?.Value;
			return match != null;
		}

		internal void SetValue(Guid jobInfoId, string nodeId, string key, string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			List<OrchestrationNodeContextValue> matches = ReadDom(CreateKeyFilter(jobInfoId, nodeId, key)).ToList();
			OrchestrationNodeContextValue target = SelectNewest(matches);

			if (target == null)
			{
				if (Count(CreateNodeFilter(jobInfoId, nodeId)) >= MaxKeysPerNode)
				{
					throw new InvalidOperationException($"Node '{nodeId}' already holds the maximum of {MaxKeysPerNode} keys.");
				}

				target = new OrchestrationNodeContextValue
				{
					JobInfoReference = jobInfoId,
					NodeId = nodeId,
					Key = key,
				};
			}

			target.Value = value;
			CreateOrUpdate(target);

			// Concurrent first writes of the same key can create duplicates; the written instance wins.
			Delete(matches.Where(x => x.ID != target.ID).ToList());
		}

		internal void RemoveValue(Guid jobInfoId, string nodeId, string key)
		{
			Delete(ReadDom(CreateKeyFilter(jobInfoId, nodeId, key)).ToList());
		}

		internal void DeleteForJobs(IEnumerable<Guid> jobInfoIds)
		{
			if (jobInfoIds == null)
			{
				throw new ArgumentNullException(nameof(jobInfoIds));
			}

			List<OrchestrationNodeContextValue> toDelete = FilterQueryExecutor.RetrieveFilteredItems(
					jobInfoIds.Where(x => x != Guid.Empty),
					x => DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.ContextValue.JobInformation).Equal(x),
					x => ReadDom(x))
				.ToList();

			Delete(toDelete);
		}

		protected internal override OrchestrationNodeContextValue CreateInstance(DomInstance domInstance)
		{
			return new OrchestrationNodeContextValue(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<OrchestrationNodeContextValue> instances)
		{
			foreach (OrchestrationNodeContextValue instance in instances)
			{
				if (instance.JobInfoReference == ApiObjectReference<OrchestrationJobInfo>.Empty)
				{
					throw new ArgumentException("A context value must reference a job.", nameof(instances));
				}

				if (String.IsNullOrEmpty(instance.NodeId))
				{
					throw new ArgumentException("A context value must reference a node.", nameof(instances));
				}

				if (String.IsNullOrEmpty(instance.Key))
				{
					throw new ArgumentException("The key of a context value cannot be empty.", nameof(instances));
				}

				if (instance.Key.Length > MaxKeyLength)
				{
					throw new ArgumentException($"Key '{instance.Key}' exceeds {MaxKeyLength} characters.", nameof(instances));
				}

				if (instance.Value == null)
				{
					throw new ArgumentException($"Value of key '{instance.Key}' cannot be null.", nameof(instances));
				}

				if (instance.Value.Length > MaxValueLength)
				{
					throw new ArgumentException($"Value of key '{instance.Key}' exceeds {MaxValueLength} characters.", nameof(instances));
				}
			}
		}

		protected override void ValidateBeforeDelete(ICollection<OrchestrationNodeContextValue> instances)
		{
		}

		private static FilterElement<DomInstance> CreateNodeFilter(Guid jobInfoId, string nodeId)
		{
			if (String.IsNullOrEmpty(nodeId))
			{
				throw new ArgumentException($"'{nameof(nodeId)}' cannot be null or empty.", nameof(nodeId));
			}

			return DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.ContextValue.JobInformation).Equal(jobInfoId)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.ContextValue.NodeID).Equal(nodeId));
		}

		private static FilterElement<DomInstance> CreateKeyFilter(Guid jobInfoId, string nodeId, string key)
		{
			if (String.IsNullOrEmpty(key))
			{
				throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
			}

			return CreateNodeFilter(jobInfoId, nodeId)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.ContextValue.Key).Equal(key));
		}

		private static OrchestrationNodeContextValue SelectNewest(IEnumerable<OrchestrationNodeContextValue> instances)
		{
			return instances
				.OrderByDescending(x => x.LastModified)
				.ThenBy(x => x.ID)
				.FirstOrDefault();
		}
	}
}
