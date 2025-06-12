namespace Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;

	public partial class ConnectionSection
	{
		public List<LevelMapping> LevelMappingList
		{
			get;
			private set;
		}

		protected override void AfterLoad()
		{
			if (String.IsNullOrEmpty(LevelMapping))
			{
				LevelMappingList = new List<LevelMapping>();
			}
			else
			{
				LevelMappingList = JsonConvert.DeserializeObject<List<LevelMapping>>(LevelMapping);
			}
		}

		protected override void BeforeToSection()
		{
			LevelMapping = LevelMappingList != null && LevelMappingList.Any()
				? JsonConvert.SerializeObject(LevelMappingList)
				: null;
		}
	}
}