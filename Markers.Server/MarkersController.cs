using JetBrains.Annotations;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Server.Communications;
using NFive.SDK.Server.Controllers;
using Addemod.Markers.Shared;
using Addemod.Markers.Shared.Models;
using System.Collections.Generic;
using System;
using Addemod.Markers.Server.Storage;
using System.Linq;
using Addemod.Markers.Server.Models;

namespace Addemod.Markers.Server
{
	[PublicAPI]
	public class MarkersController : ConfigurableController<Configuration>
	{
		private ICommunicationManager comms;
		private MarkersManager markerManager;

		public MarkersController(ILogger logger, Configuration configuration, ICommunicationManager comms, MarkersManager markerManager) : base(logger, configuration)
		{
			this.comms = comms;
			this.markerManager = markerManager;

			// Send configuration when requested
			comms.Event(MarkersEvents.Configuration).FromClients().OnRequest(e => e.Reply(this.Configuration));

			comms.Event(MarkersEvents.GetAllMarkers).FromClients().OnRequest(GetAllMarkersEvent);
			if (this.Configuration.editMode) {
				comms.Event(MarkersEvents.AddMarker).FromClients().OnRequest<Marker>(AddMarkerEvent);
				comms.Event(MarkersEvents.RemoveMarker).FromClients().OnRequest<Guid>(RemoveMarkerEvent);
			}
		}

		public override void Reload(Configuration configuration) {
			base.Reload(configuration);

			this.comms.Event(MarkersEvents.Reload).ToClients().Emit();
		}

		private async void AddMarkerEvent(ICommunicationMessage e, Marker marker) {
			try {
				await markerManager.AddMarker(marker);
				e.Reply(markerManager.GetAllMarkers());
			} catch {
				e.Reply(null);
			}
		}

		private async void RemoveMarkerEvent(ICommunicationMessage e, Guid markerId) {
			try {
				await markerManager.RemoveMarker(markerId);
				e.Reply(markerManager.GetAllMarkers());
			} catch {
				e.Reply(null);
			}
		}

		private void GetAllMarkersEvent(ICommunicationMessage e) {
			e.Reply(markerManager.GetAllMarkers());
		}
	}
}
