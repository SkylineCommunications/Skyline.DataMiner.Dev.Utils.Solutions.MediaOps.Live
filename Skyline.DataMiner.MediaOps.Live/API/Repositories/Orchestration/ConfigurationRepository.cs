namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	internal class ConfigurationRepository : Repository<Configuration>
	{
		internal ConfigurationRepository(MediaOpsLiveApi api) : base(api, api.SlcOrchestrationHelper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => Configuration.DomDefinition;

		public void DeleteConfiguration(Guid domInstanceId)
		{
			Delete(GetByDomInstanceId(domInstanceId));
		}

		private Configuration GetByDomInstanceId(Guid domInstanceId)
		{
			ManagedFilter<DomInstance, Guid> filter = DomInstanceExposers.Id.Equal(domInstanceId);

			List<Configuration> result = Read(filter).ToList();

			if (!result.Any())
			{
				return null;
			}

			return result[0];
		}

		protected internal override Configuration CreateInstance(DomInstance domInstance)
		{
			return new Configuration(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<Configuration> instances)
		{
		}

		protected override void ValidateBeforeDelete(ICollection<Configuration> instances)
		{
		}
	}
}
