namespace CheckMediaOpsLive
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;

	using CheckMediaOpsLive.Automation;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	using Net = Skyline.DataMiner.Net;

	public sealed class ErrorAnalyzer
	{
		private readonly MediaOpsLiveApi _api;
		private readonly VirtualSignalGroupsData _data;
		private readonly AutomationScriptValidator _scriptValidator;

		public ErrorAnalyzer(MediaOpsLiveApi api, Net.IConnection connection)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			_api = api;

			_data = new VirtualSignalGroupsData(api);
			_scriptValidator = new AutomationScriptValidator(connection);
		}

		public ICollection<Error> Errors { get; } = [];

		public void Analyze()
		{
			try
			{
				CheckEndpoints();
				CheckVirtualSignalGroups();

				CheckMediationElements();
				CheckConnectionHandlerScripts();

				DetectUnassignedEndpoints();
				DetectEmptyVirtualSignalGroups();
			}
			catch (Exception ex)
			{
				AddException(ex);
			}
		}

		private void CheckEndpoints()
		{
			try
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
			catch (Exception ex)
			{
				AddException(ex);
			}
		}

		private void CheckVirtualSignalGroups()
		{
			try
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
			catch (Exception ex)
			{
				AddException(ex);
			}
		}

		private void CheckMediationElements()
		{
			try
			{
				var dms = _api.GetDms();
				var mediationElements = _api.MediationElements.GetAllElements();

				foreach (var me in mediationElements)
				{
					var dmsElement = dms.GetElement(me.DmsElementId);

					if (dmsElement.State != ElementState.Active)
					{
						continue;
					}

					var activeAlarmsCount = dmsElement.GetActiveAlarmCount();

					if (activeAlarmsCount > 0)
					{
						AddWarning(
							$"Mediation element '{me.Name}' (ID: {me.DmaId}/{me.ElementId}) has {activeAlarmsCount} active alarm(s).",
							new
							{
								Element = new { me.DmsElementId, me.Name },
								ActiveAlarms = activeAlarmsCount,
							});
					}
				}
			}
			catch (Exception ex)
			{
				AddException(ex);
			}
		}

		private void CheckConnectionHandlerScripts()
		{
			try
			{
				var mediationElements = _api.MediationElements.GetAllElements();
				var allConnectionHandlerScripts = mediationElements.SelectMany(x => x.GetConnectionHandlerScriptNames()).ToHashSet();
				var mediatedElements = mediationElements.SelectMany(x => x.GetMediatedElements()).ToList();

				foreach (var me in mediatedElements)
				{
					if (String.IsNullOrEmpty(me.ConnectionHandlerScript))
					{
						AddWarning(
							$"Mediated element '{me.Name}' (ID: {me.Id.AgentId}/{me.Id.ElementId}) does not have a connection handler script assigned.",
							new
							{
								Element = new { me.Id, me.Name },
							});
					}

					if (!allConnectionHandlerScripts.Contains(me.ConnectionHandlerScript))
					{
						AddError(
							$"Mediated element '{me.Name}' (ID: {me.Id.AgentId}/{me.Id.ElementId}) has an invalid connection handler script assigned.",
							new
							{
								Element = new { me.Id, me.Name },
								me.ConnectionHandlerScript,
							});
					}
				}

				foreach (var script in allConnectionHandlerScripts)
				{
					var isUsed = mediatedElements.Any(x => x.ConnectionHandlerScript == script);
					if (!isUsed)
					{
						AddWarning(
							$"Connection handler script '{script}' is not used by any mediated element.",
							new
							{
								Script = script,
							});
					}

					if (!_scriptValidator.ValidateScript(script, out var scriptErrors))
					{
						AddError(
							$"Connection handler script '{script}' has syntax errors.",
							new
							{
								Script = script,
								Errors = scriptErrors,
							});
					}
				}
			}
			catch (Exception ex)
			{
				AddException(ex);
			}
		}

		private void DetectUnassignedEndpoints()
		{
			try
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
							unassignedEndpoints.Count,
							Endpoints = unassignedEndpoints.Select(x => new { x.ID, x.Name })
						});
				}
			}
			catch (Exception ex)
			{
				AddException(ex);
			}
		}

		private void DetectEmptyVirtualSignalGroups()
		{
			try
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
							emptyVSGs.Count,
							VirtualSignalGroups = emptyVSGs.Select(x => new { x.ID, x.Name })
						});
				}
			}
			catch (Exception ex)
			{
				AddException(ex);
			}
		}

		private void AddWarning(string text, object details = null)
		{
			Errors.Add(new Error(ErrorSeverity.Warning, text) { Details = details });
		}

		private void AddError(string text, object details = null)
		{
			Errors.Add(new Error(ErrorSeverity.Error, text) { Details = details });
		}

		private void AddException(Exception ex, [CallerMemberName] string methodName = "")
		{
			AddError(
				$"An exception occurred in method '{methodName}': {ex.Message}",
				new
				{
					Method = methodName,
					Exception = new { ex.Message, ex.StackTrace },
				});
		}
	}
}
