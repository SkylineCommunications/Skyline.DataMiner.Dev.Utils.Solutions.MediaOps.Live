namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.SlcOrchestration
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class ConfigurationRepository : Repository<Configuration>
	{
		public ConfigurationRepository(SlcOrchestrationHelper helper) : base(helper)
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

		protected override Configuration CreateInstance(DomInstance domInstance)
		{
			return new Configuration(domInstance);
		}
	}
}
