namespace CheckMediaOpsLive
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public sealed class ErrorAnalyzer
	{
		private readonly MediaOpsLiveApi _api;
		private readonly VirtualSignalGroupsData _data;

		public ErrorAnalyzer(MediaOpsLiveApi api)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
			_data = new VirtualSignalGroupsData(api);
		}

		public ICollection<Error> Errors { get; } = [];

		public void Analyze()
		{
			CheckEndpoints();
			CheckVirtualSignalGroups();

			DetectUnassignedEndpoints();
			DetectEmptyVirtualSignalGroups();
		}

		private void CheckEndpoints()
		{
			var dms = _api.GetDms();
			var elements = dms.GetElements().ToDictionary(x => x.DmsElementId);

			foreach (var endpoint in _data.Endpoints.Values)
			{
				if (endpoint.TransportType.HasValue &&
					!_data.TransportTypes.ContainsKey(endpoint.TransportType.Value))
				{
					AddError(
						$"Endpoint '{endpoint.Name}' (ID: {endpoint.ID}) has an invalid transport type reference.",
						new
						{
							Endpoint = new { endpoint.ID, endpoint.Name },
							TransportTypeReference = endpoint.TransportType.Value
						});
				}

				if (endpoint.Element.HasValue &&
					!elements.ContainsKey(endpoint.Element.Value))
				{
					AddError(
						$"Endpoint '{endpoint.Name}' (ID: {endpoint.ID}) has an invalid element reference.",
						new
						{
							Endpoint = new { endpoint.ID, endpoint.Name },
							ElementReference = endpoint.Element.Value
						});
				}

				if (endpoint.ControlElement.HasValue &&
					!elements.ContainsKey(endpoint.ControlElement.Value))
				{
					AddError(
						$"Endpoint '{endpoint.Name}' (ID: {endpoint.ID}) has an invalid control element reference.",
						new
						{
							Endpoint = new { endpoint.ID, endpoint.Name },
							ControlElementReference = endpoint.ControlElement.Value
						});
				}
			}
		}

		private void CheckVirtualSignalGroups()
		{
			foreach (var vsg in _data.VirtualSignalGroups.Values)
			{
				foreach (var levelEndpoint in vsg.Levels)
				{
					if (!_data.Levels.ContainsKey(levelEndpoint.Level))
					{
						AddError(
							$"Virtual Signal Group '{vsg.Name}' (ID: {vsg.ID}) has an invalid level reference.",
							new
							{
								VirtualSignalGroup = new { vsg.ID, vsg.Name },
								LevelReference = levelEndpoint
							});
					}

					if (!_data.Endpoints.ContainsKey(levelEndpoint.Endpoint))
					{
						AddError(
							$"Virtual Signal Group '{vsg.Name}' (ID: {vsg.ID}) has an invalid endpoint reference.",
							new
							{
								VirtualSignalGroup = new { vsg.ID, vsg.Name },
								EndpointReference = levelEndpoint
							});
					}
				}
			}
		}

		private void DetectUnassignedEndpoints()
		{
			var unassignedEndpoints = new List<Endpoint>();

			foreach (var endpoint in _data.Endpoints.Values)
			{
				var virtualSignalGroups = _data.Mapping.GetVirtualSignalGroups(endpoint);

				if (virtualSignalGroups.Count == 0)
				{
					unassignedEndpoints.Add(endpoint);
				}
			}

			if (unassignedEndpoints.Count > 0)
			{
				var message = unassignedEndpoints.Count == 1
					? $"There is 1 unassigned endpoint."
					: $"There are {unassignedEndpoints.Count} unassigned endpoints.";

				AddWarning(
					message,
					new
					{
						Count = unassignedEndpoints.Count,
						Endpoints = unassignedEndpoints.Select(x => new { x.ID, x.Name })
					});
			}
		}

		private void DetectEmptyVirtualSignalGroups()
		{
			var emptyVSGs = new List<VirtualSignalGroup>();

			foreach (var vsg in _data.VirtualSignalGroups.Values)
			{
				var endpoints = _data.Mapping.GetEndpoints(vsg);
				if (endpoints.Count == 0)
				{
					emptyVSGs.Add(vsg);
				}
			}

			if (emptyVSGs.Count > 0)
			{
				var message = emptyVSGs.Count == 1
					? $"There is 1 empty virtual signal group."
					: $"There are {emptyVSGs.Count} empty virtual signal groups.";

				AddWarning(
					message,
					new
					{
						Count = emptyVSGs.Count,
						VirtualSignalGroups = emptyVSGs.Select(x => new { x.ID, x.Name })
					});
			}
		}

		private void AddWarning(string text, object details = null)
		{
			Errors.Add(new Error(Error.ErrorSeverity.Warning, text) { Details = details });
		}

		private void AddError(string text, object details = null)
		{
			Errors.Add(new Error(Error.ErrorSeverity.Error, text) { Details = details });
		}
	}
}
