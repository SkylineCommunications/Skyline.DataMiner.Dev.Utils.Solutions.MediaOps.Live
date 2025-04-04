namespace Skyline.DataMiner.MediaOps.Live.DOM.Helpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Model.SlcOrchestration;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class SlcOrchestrationHelper : DomModuleHelperBase
	{
		public SlcOrchestrationHelper(Func<DMSMessage[], DMSMessage[]> messageHandler) : base(SlcOrchestrationIds.ModuleId, messageHandler)
		{
		}

		public SlcOrchestrationHelper(IEngine engine) : base(SlcOrchestrationIds.ModuleId, engine)
		{
		}

		#region Virtual Signal Groups

		public IEnumerable<OrchestrationEventInstance> GetAllOrchestrationEvents()
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id);

			return GetOrchestrationEventIterator(filter);
		}

		#endregion

		#region Iterators

		private IEnumerable<OrchestrationEventInstance> GetOrchestrationEventIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, x => new OrchestrationEventInstance(x));
		}

		#endregion
	}
}
