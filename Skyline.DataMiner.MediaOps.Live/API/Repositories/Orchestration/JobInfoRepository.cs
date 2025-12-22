namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using SLDataGateway.API.Types.Querying;

	internal class JobInfoRepository : Repository<OrchestrationJobInfo>
	{
		internal JobInfoRepository(MediaOpsLiveApi api) : base(api, api.SlcOrchestrationHelper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => OrchestrationJobInfo.DomDefinition;

		public void DeleteOrchestrationJobInfo(Guid domInstanceId)
		{
			Delete(GetByDomInstanceId(domInstanceId));
		}

		private OrchestrationJobInfo GetByDomInstanceId(Guid domInstanceId)
		{
			ManagedFilter<DomInstance, Guid> filter = DomInstanceExposers.Id.Equal(domInstanceId);

			List<OrchestrationJobInfo> result = ReadDom(filter).ToList();

			if (!result.Any())
			{
				return null;
			}

			return result[0];
		}

		internal OrchestrationJobInfo GetJobInfoByJobReference(string jobReference)
		{
			if (String.IsNullOrEmpty(jobReference))
			{
				throw new ArgumentException($"'{nameof(jobReference)}' cannot be null or empty", nameof(jobReference));
			}

			return Read(OrchestrationJobInfoExposers.JobReference.Equal(jobReference)).FirstOrDefault();
		}

		internal Dictionary<string, OrchestrationJobInfo> GetJobInfosByJobReference(IEnumerable<string> jobReferences)
		{
			ORFilterElement<OrchestrationJobInfo> filter = new ORFilterElement<OrchestrationJobInfo>(jobReferences.Distinct().Select(reference => OrchestrationJobInfoExposers.JobReference.Equal(reference)).ToArray());

			return Read(filter).ToDictionary(x => x.JobReference);
		}

		protected internal override OrchestrationJobInfo CreateInstance(DomInstance domInstance)
		{
			return new OrchestrationJobInfo(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<OrchestrationJobInfo> instances)
		{
		}

		protected override void ValidateBeforeDelete(ICollection<OrchestrationJobInfo> instances)
		{
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(OrchestrationJobInfo.JobReference):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.JobInfo.JobReference), comparer, value);
			}

			return base.CreateFilter(fieldName, comparer, value);
		}

		protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(OrchestrationJobInfo.JobReference):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.JobInfo.JobReference), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}
	}
}