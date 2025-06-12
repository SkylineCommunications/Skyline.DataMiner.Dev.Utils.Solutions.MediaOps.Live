namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class ConfigurationRepository : Repository<Configuration>
	{
		internal ConfigurationRepository(SlcOrchestrationHelper helper, IConnection connection) : base(helper, connection)
		{
		}

		protected internal override DomDefinitionId DomDefinition => Configuration.DomDefinition;

		public void DeleteConfiguration(Guid domInstanceId)
		{
			Delete(GetByDomInstanceId(domInstanceId));
		}

		public Configuration GetByDomInstanceId(Guid domInstanceId)
		{
			var filter = DomInstanceExposers.Id.Equal(domInstanceId);

			var result = Read(filter);

			if (result == null || !result.Any())
			{
				return null;
			}

			return result.First();
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
