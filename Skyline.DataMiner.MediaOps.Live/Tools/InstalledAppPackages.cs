namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.AppPackages;
	using Skyline.DataMiner.Net.AppPackages.Messages;

	internal class InstalledAppPackages
	{
		private readonly IConnection _connection;

		private readonly ConcurrentDictionary<string, InstalledAppInfo> _installedAppPackagesCache = new(StringComparer.OrdinalIgnoreCase);

		private readonly object _loadLock = new();
		private DateTimeOffset _lastLoaded;

		public InstalledAppPackages(IConnection connection)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		public IEnumerable<InstalledAppInfo> GetAllInstalledPackages()
		{
			RefreshCache();

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

			// Ensure cache is loaded
			RefreshCache();

			return _installedAppPackagesCache.TryGetValue(appPackageName, out installedAppInfo) &&
				   installedAppInfo.InstallState.InstallStatus == AppInstallStatus.INSTALLED;
		}

		public bool IsInstalled(string appPackageName)
		{
			return IsInstalled(appPackageName, out _);
		}

		private void RefreshCache()
		{
			lock (_loadLock)
			{
				var now = DateTimeOffset.UtcNow;

				// Throttle reloads to at most once per minute
				if (now - _lastLoaded <= TimeSpan.FromMinutes(1))
				{
					return;
				}

				LoadInstalledAppPackages();
				_lastLoaded = now;
			}
		}

		private void LoadInstalledAppPackages()
		{
			var request = new GetInstalledAppPackagesRequest();
			var response = (GetInstalledAppPackagesResponse)_connection.HandleSingleResponseMessage(request);

			foreach (var appPackage in response.InstalledAppPackages)
			{
				_installedAppPackagesCache[appPackage.AppInfo.Name] = appPackage;
			}
		}
	}
}
