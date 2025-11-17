namespace Skyline.DataMiner.MediaOps.Live.Tests
{
	using System.Reflection;

	using PublicApiGenerator;

	[TestClass]
	[UsesVerify]
	public sealed partial class MediaOps_LiveApi_Tests_Api_PublicChanges
	{
		[TestMethod]
		public Task MediaOps_LiveApi_Tests_Api_NoPublicApiChanges_Common()
		{
			var assemblyName = "Skyline.DataMiner.MediaOps.Live";
			var publicApi = Assembly.Load(assemblyName).GeneratePublicApi();

			return Verifier.Verify(publicApi)
				.UseFileName($"{assemblyName}_PublicApi")
				.AutoVerify();
		}

		[TestMethod]
		public Task MediaOps_LiveApi_Tests_Api_NoPublicApiChanges_Automation()
		{
			var assemblyName = "Skyline.DataMiner.MediaOps.Live.Automation";
			var publicApi = Assembly.Load(assemblyName).GeneratePublicApi();

			return Verifier.Verify(publicApi)
				.UseFileName($"{assemblyName}_PublicApi")
				.AutoVerify();
		}

		[TestMethod]
		public Task MediaOps_LiveApi_Tests_Api_NoPublicApiChanges_Protocol()
		{
			var assemblyName = "Skyline.DataMiner.MediaOps.Live.Protocol";
			var publicApi = Assembly.Load(assemblyName).GeneratePublicApi();

			return Verifier.Verify(publicApi)
				.UseFileName($"{assemblyName}_PublicApi")
				.AutoVerify();
		}

		[TestMethod]
		public Task MediaOps_LiveApi_Tests_Api_NoPublicApiChanges_GQI()
		{
			var assemblyName = "Skyline.DataMiner.MediaOps.Live.GQI";
			var publicApi = Assembly.Load(assemblyName).GeneratePublicApi();

			return Verifier.Verify(publicApi)
				.UseFileName($"{assemblyName}_PublicApi")
				.AutoVerify();
		}
	}
}
