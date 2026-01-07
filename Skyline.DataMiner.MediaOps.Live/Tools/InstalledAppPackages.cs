namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.AppPackages;
	using Skyline.DataMiner.Net.AppPackages.Messages;

	internal class InstalledAppPackages
	{
		private readonly IConnection _connection;

		private readonly Dictionary<string, InstalledAppInfo> _installedAppPackagesCache = new(StringComparer.OrdinalIgnoreCase);

		private readonly object _loadLock = new();

		public InstalledAppPackages(IConnection connection)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		public IEnumerable<InstalledAppInfo> GetAllInstalledPackages()
		{
			LoadInstalledAppPackages();

			foreach (var installedApp in _installedAppPackagesCache.Values)
			{
				if (installedApp.InstallState.InstallStatus == AppInstallStatus.INSTALLED)
				{
					yield return installedApp;
				}
			}
		}

		public bool IsInstalled(string appPackageName, out InstalledAppInfo installedAppInfo)
		{
			if (String.IsNullOrWhiteSpace(appPackageName))
			{
				throw new ArgumentException("Value cannot be null or whitespace.", nameof(appPackageName));
			}

			if (_installedAppPackagesCache.TryGetValue(appPackageName, out installedAppInfo) &&
				installedAppInfo.InstallState.InstallStatus == AppInstallStatus.INSTALLED)
			{
				// Already cached and installed
				return true;
			}

			// Reload cache
			LoadInstalledAppPackages();

			return _installedAppPackagesCache.TryGetValue(appPackageName, out installedAppInfo) &&
				   installedAppInfo.InstallState.InstallStatus == AppInstallStatus.INSTALLED;
		}

		public bool IsInstalled(string appPackageName)
		{
			return IsInstalled(appPackageName, out _);
		}

		private void LoadInstalledAppPackages()
		{
			lock (_loadLock)
			{
				var request = new GetInstalledAppPackagesRequest();
				var response = (GetInstalledAppPackagesResponse)_connection.HandleSingleResponseMessage(request);

				_installedAppPackagesCache.Clear();

				foreach (var appPackage in response.InstalledAppPackages)
				{
					_installedAppPackagesCache[appPackage.AppInfo.Name] = appPackage;
				}
			}
		}
	}
}
