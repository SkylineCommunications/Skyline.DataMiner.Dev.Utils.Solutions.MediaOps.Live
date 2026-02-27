namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests
{
	using System.Reflection;

	using PublicApiGenerator;

	[TestClass]
	[UsesVerify]
	public sealed partial class MediaOps_LiveApi_Tests_Api_PublicChanges
	{
		private const string RootAssemblyName = "Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Live";

		[TestMethod]
		public Task MediaOps_LiveApi_Tests_Api_NoPublicApiChanges_Common()
		{
			var assemblyName = RootAssemblyName;
			var publicApi = Assembly.Load(assemblyName).GeneratePublicApi();

			return Verifier.Verify(publicApi)
				.UseFileName($"{assemblyName}_PublicApi")
				.AutoVerify(includeBuildServer: false);
		}

		[TestMethod]
		public Task MediaOps_LiveApi_Tests_Api_NoPublicApiChanges_Plan()
		{
			var assemblyName = $"{RootAssemblyName}.Plan";
			var publicApi = Assembly.Load(assemblyName).GeneratePublicApi();

			return Verifier.Verify(publicApi)
				.UseFileName($"{assemblyName}_PublicApi")
				.AutoVerify(includeBuildServer: false);
		}

		[TestMethod]
		public Task MediaOps_LiveApi_Tests_Api_NoPublicApiChanges_Automation()
		{
			var assemblyName = $"{RootAssemblyName}.Automation";
			var publicApi = Assembly.Load(assemblyName).GeneratePublicApi();

			return Verifier.Verify(publicApi)
				.UseFileName($"{assemblyName}_PublicApi")
				.AutoVerify(includeBuildServer: false);
		}

		[TestMethod]
		public Task MediaOps_LiveApi_Tests_Api_NoPublicApiChanges_Protocol()
		{
			var assemblyName = $"{RootAssemblyName}.Protocol";
			var publicApi = Assembly.Load(assemblyName).GeneratePublicApi();

			return Verifier.Verify(publicApi)
				.UseFileName($"{assemblyName}_PublicApi")
				.AutoVerify(includeBuildServer: false);
		}

		[TestMethod]
		public Task MediaOps_LiveApi_Tests_Api_NoPublicApiChanges_GQI()
		{
			var assemblyName = $"{RootAssemblyName}.GQI";
			var publicApi = Assembly.Load(assemblyName).GeneratePublicApi();

			return Verifier.Verify(publicApi)
				.UseFileName($"{assemblyName}_PublicApi")
				.AutoVerify(includeBuildServer: false);
		}
	}
}
