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

	internal class NodePhasesRepository : Repository<OrchestrationNodePhases>
	{
		internal NodePhasesRepository(MediaOpsLiveApi api) : base(api, api.SlcOrchestrationHelper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => OrchestrationNodePhases.DomDefinition;

		internal OrchestrationNodePhases GetNodePhases(Guid eventId, string nodeId)
		{
			return SelectNewest(ReadDom(CreateEventNodeFilter(eventId, nodeId)));
		}

		internal IReadOnlyDictionary<string, OrchestrationNodePhases> GetNodePhasesForEvent(Guid eventId)
		{
			return ReadDom(CreateEventFilter(eventId))
				.GroupBy(x => x.NodeId)
				.ToDictionary(g => g.Key, SelectNewest);
		}

		/// <summary>
		/// Gets the nodes of the event that wait for a scheduled resume.
		/// </summary>
		internal IReadOnlyCollection<OrchestrationNodePhases> GetPendingResumes(Guid eventId)
		{
			return GetNodePhasesForEvent(eventId).Values
				.Where(x => x.ResumeSchedulerReference != null)
				.ToList();
		}

		/// <summary>
		/// Returns the stored phases of the node for the event, or a new unsaved instance when none exist yet.
		/// Duplicates left behind by concurrent creates are removed.
		/// </summary>
		internal OrchestrationNodePhases GetOrCreateNodePhases(Guid jobInfoId, Guid eventId, string nodeId)
		{
			List<OrchestrationNodePhases> matches = ReadDom(CreateEventNodeFilter(eventId, nodeId)).ToList();
			OrchestrationNodePhases newest = SelectNewest(matches);

			if (newest == null)
			{
				return new OrchestrationNodePhases
				{
					JobInfoReference = jobInfoId,
					EventReference = eventId,
					NodeId = nodeId,
				};
			}

			Delete(matches.Where(x => x.ID != newest.ID).ToList());

			return newest;
		}

		internal IReadOnlyCollection<OrchestrationNodePhases> DeleteForEvents(IEnumerable<Guid> eventIds)
		{
			if (eventIds == null)
			{
				throw new ArgumentNullException(nameof(eventIds));
			}

			List<OrchestrationNodePhases> toDelete = FilterQueryExecutor.RetrieveFilteredItems(
					eventIds.Where(x => x != Guid.Empty),
					CreateEventFilter,
					x => ReadDom(x))
				.ToList();

			Delete(toDelete);

			return toDelete;
		}

		protected internal override OrchestrationNodePhases CreateInstance(DomInstance domInstance)
		{
			return new OrchestrationNodePhases(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<OrchestrationNodePhases> instances)
		{
			foreach (OrchestrationNodePhases instance in instances)
			{
				if (instance.JobInfoReference == ApiObjectReference<OrchestrationJobInfo>.Empty)
				{
					throw new ArgumentException("Node phases must reference a job.", nameof(instances));
				}

				if (instance.EventReference == ApiObjectReference<OrchestrationEvent>.Empty)
				{
					throw new ArgumentException("Node phases must reference an orchestration event.", nameof(instances));
				}

				if (String.IsNullOrEmpty(instance.NodeId))
				{
					throw new ArgumentException("Node phases must reference a node.", nameof(instances));
				}

				if (instance.Phases.Any(x => String.IsNullOrEmpty(x.Key)))
				{
					throw new ArgumentException($"A phase of node '{instance.NodeId}' has no key.", nameof(instances));
				}

				string duplicateKey = instance.Phases
					.GroupBy(x => x.Key, StringComparer.Ordinal)
					.FirstOrDefault(g => g.Count() > 1)?.Key;

				if (duplicateKey != null)
				{
					throw new ArgumentException($"Phase '{duplicateKey}' is defined more than once for node '{instance.NodeId}'.", nameof(instances));
				}
			}
		}

		protected override void ValidateBeforeDelete(ICollection<OrchestrationNodePhases> instances)
		{
		}

		private static FilterElement<DomInstance> CreateEventFilter(Guid eventId)
		{
			return DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.NodePhasesInfo.OrchestrationEvent).Equal(eventId);
		}

		private static FilterElement<DomInstance> CreateEventNodeFilter(Guid eventId, string nodeId)
		{
			if (String.IsNullOrEmpty(nodeId))
			{
				throw new ArgumentException($"'{nameof(nodeId)}' cannot be null or empty.", nameof(nodeId));
			}

			return CreateEventFilter(eventId)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.NodePhasesInfo.NodeID).Equal(nodeId));
		}

		private static OrchestrationNodePhases SelectNewest(IEnumerable<OrchestrationNodePhases> instances)
		{
			return instances
				.OrderByDescending(x => x.LastModified)
				.ThenBy(x => x.ID)
				.FirstOrDefault();
		}
	}
}
