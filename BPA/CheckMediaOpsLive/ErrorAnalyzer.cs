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
		private readonly VirtualSignalGroupsContext _data;
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

			_data = new VirtualSignalGroupsContext(api);
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

				var invalidTransportTypeReference = new List<Endpoint>();
				var invalidElementReference = new List<Endpoint>();
				var invalidControlElementReference = new List<Endpoint>();
				var unassignedEndpoints = new List<Endpoint>();

				foreach (var endpoint in _data.Endpoints.Values)
				{
					if (endpoint.TransportType.HasValue &&
						!_data.TransportTypes.ContainsKey(endpoint.TransportType.Value))
					{
						invalidTransportTypeReference.Add(endpoint);
					}

					if (endpoint.Element.HasValue &&
						!elements.ContainsKey(endpoint.Element.Value))
					{
						invalidElementReference.Add(endpoint);
					}

					if (endpoint.ControlElement.HasValue &&
						!elements.ContainsKey(endpoint.ControlElement.Value))
					{
						invalidControlElementReference.Add(endpoint);
					}

					var virtualSignalGroups = _data.Mapping.GetVirtualSignalGroups(endpoint);
					if (virtualSignalGroups.Count == 0)
					{
						unassignedEndpoints.Add(endpoint);
					}
				}

				if (invalidTransportTypeReference.Count > 0)
				{
					AddError(
						invalidTransportTypeReference.Count == 1
							? $"One endpoint has an invalid transport type reference."
							: $"{invalidTransportTypeReference.Count} endpoints have an invalid transport type reference.",
						new { invalidTransportTypeReference.Count, });
				}

				if (invalidElementReference.Count > 0)
				{
					AddError(
						invalidElementReference.Count == 1
							? $"One endpoint has an invalid element reference."
							: $"{invalidElementReference.Count} endpoints have an invalid element reference.",
						new { invalidElementReference.Count, });
				}

				if (invalidControlElementReference.Count > 0)
				{
					AddError(
						invalidControlElementReference.Count == 1
							? $"One endpoint has an invalid control element reference."
							: $"{invalidControlElementReference.Count} endpoints have an invalid control element reference.",
						new { invalidControlElementReference.Count, });
				}

				if (unassignedEndpoints.Count > 0)
				{
					AddWarning(
						unassignedEndpoints.Count == 1
							? $"One endpoint is not assigned to any VSG."
							: $"{unassignedEndpoints.Count} endpoints are not assigned to any VSG.",
						new { unassignedEndpoints.Count, });
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
				var invalidLevelReference = new List<VirtualSignalGroup>();
				var invalidEndpointReference = new List<VirtualSignalGroup>();
				var emptyVSGs = new List<VirtualSignalGroup>();

				foreach (var vsg in _data.VirtualSignalGroups.Values)
				{
					foreach (var levelEndpoint in vsg.Levels)
					{
						if (!_data.Levels.ContainsKey(levelEndpoint.Level))
						{
							invalidLevelReference.Add(vsg);
						}

						if (!_data.Endpoints.ContainsKey(levelEndpoint.Endpoint))
						{
							invalidEndpointReference.Add(vsg);
						}
					}

					var endpoints = _data.Mapping.GetEndpoints(vsg);
					if (endpoints.Count == 0)
					{
						emptyVSGs.Add(vsg);
					}
				}

				if (invalidLevelReference.Count > 0)
				{
					AddError(
						invalidLevelReference.Count == 1
							? $"One VSG has an invalid level reference."
							: $"{invalidLevelReference.Count} VSGs have an invalid level reference.",
						new { invalidLevelReference.Count, });
				}

				if (invalidEndpointReference.Count > 0)
				{
					AddError(
						invalidEndpointReference.Count == 1
							? $"One VSG has an invalid endpoint reference."
							: $"{invalidEndpointReference.Count} VSGs have an invalid endpoint reference.",
						new { invalidEndpointReference.Count, });
				}

				if (emptyVSGs.Count > 0)
				{
					AddWarning(
						emptyVSGs.Count == 1
							? $"One VSG does not have any endpoints assigned."
							: $"{emptyVSGs.Count} VSGs don't have any endpoints assigned.",
						new { emptyVSGs.Count, });
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
