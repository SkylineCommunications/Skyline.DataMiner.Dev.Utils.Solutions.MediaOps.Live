namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.Orchestration
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	internal class JobInfoRepository : Repository<OrchestrationJobInfo>
	{
		internal JobInfoRepository(SlcOrchestrationHelper helper, IConnection connection) : base(helper, connection)
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

			List<OrchestrationJobInfo> result = Read(filter).ToList();

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

			ManagedFilter<DomInstance, IEnumerable> filter = DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.JobInfo.JobReference).Equal(jobReference);

			return Read(filter).FirstOrDefault();
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
	}
}