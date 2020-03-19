using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NFive.SDK.Client.Commands;
using NFive.SDK.Client.Communications;
using NFive.SDK.Client.Events;
using NFive.SDK.Client.Interface;
using NFive.SDK.Client.Services;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Models.Player;
using Addemod.Markers.Client.Overlays;
using Addemod.Markers.Shared;
using CitizenFX.Core.Native;
using CitizenFX.Core;
using Addemod.Markers.Client.Models;
using System.Collections.Generic;
using System.Linq;
using NFive.SDK.Client.Input;

namespace Addemod.Markers.Client
{
	[PublicAPI]
	public class MarkersService : Service
	{
		private Configuration config;

		private List<Marker> markers;

		// The markers the player currently is inside
		private List<Marker> insideCurrentMarkers;

		public MarkersService(ILogger logger, ITickManager ticks, ICommunicationManager comms, ICommandManager commands, IOverlayManager overlay, User user) : base(logger, ticks, comms, commands, overlay, user) { }

		public override async Task Started()
		{
			// Request server configuration
			this.config = await this.Comms.Event(MarkersEvents.Configuration).ToServer().Request<Configuration>();
			// Listen for reload event
			this.Comms.Event(MarkersEvents.Reload).FromServer().On(Reload);
			// Listen for marker clicked event
			this.Comms.Event(MarkersEvents.MarkerClicked).FromClient().On<Marker>(MarkerClickedEvent);

			Setup();
		}

		private void MarkerClickedEvent(ICommunicationMessage e, Marker marker) {
			Logger.Info("Clicked marker " + marker.Id);
		}

		private async void Setup() {
			this.config = await this.Comms.Event(MarkersEvents.Configuration).ToServer().Request<Configuration>();
			this.markers = await this.Comms.Event(MarkersEvents.GetAllMarkers).ToServer().Request<List<Marker>>();
			this.insideCurrentMarkers = new List<Marker>();

			Logger.Info(config.editMode.ToString());

			if (this.config.editMode) {
				Commands.On("addmarker", args => AddMarkerCommand(args.ToList()));
				Commands.On("removemarker", RemoveMarkerCommand);
			}

			this.Ticks.On(MarkersTick);
		}

		private void Reload(ICommunicationMessage e) {
			this.Ticks.Off(MarkersTick);
			Setup();
		}

		private async void AddMarkerCommand(List<string> args) {
			var pos = API.GetPedBoneCoords(Game.PlayerPed.Handle, (int)Bone.SKEL_L_Foot, 0, 0, -0.1f);
			var marker = new Marker() {
				Location = new NFive.SDK.Core.Models.Vector3(pos.X, pos.Y, pos.Z)
			};
			var markersList = await Comms.Event(MarkersEvents.AddMarker).ToServer().Request<List<Marker>>(marker);
			if (markersList != null) {
				this.markers = markersList;
				Logger.Info("Added marker");
			} else {
				Logger.Info("Couldn't add marker, check server logs for more info :-)");
			}
		}

		private async void RemoveMarkerCommand() {
			var pedPos = Game.PlayerPed.Position;

			// Wtf should I call it?? Of all markers that you are inside, get the closests one
			Marker closestMarkerInside = null;
			foreach(var marker in this.insideCurrentMarkers) {
				var markerLocation = ToCfxVector(marker.Location);
				var pedMarkerDistance = markerLocation.DistanceToSquared(pedPos);
				if(closestMarkerInside == null) {
					closestMarkerInside = marker;
					continue;
				} else {
					// If the currently closest marker is further away from the marker we are checking right now
					if(ToCfxVector(closestMarkerInside.Location).DistanceToSquared(pedPos) > pedMarkerDistance) {
						// Set the closest marker to the new one
						closestMarkerInside = marker;
					}
				}
			}

			if(closestMarkerInside != null) {
				// Update list of markers
				var markersList = await Comms.Event(MarkersEvents.RemoveMarker).ToServer().Request<List<Marker>>(closestMarkerInside.Id);
				if(markersList != null) {
					this.markers = markersList;
					this.insideCurrentMarkers.Remove(closestMarkerInside);
					Logger.Info("Deleted marker " + closestMarkerInside.Id);
				} else {
					Logger.Info("Couldn't delete marker, check server logs for more info :-)");
				}
			} else {
				// Not inside any marker
				Logger.Info("Tried to delete a marker, but we are not inside any? Pepega");
			}
		}

		private void MarkersTick() {
			// I thought it was needed but it was flickering, either the calculations are too heavy or it is not needed?
			//await Delay(0);
			var pedPos = Game.PlayerPed.Position;
			foreach (var marker in this.markers) {
				API.DrawMarker(marker.MarkerType,
					marker.Location.X, marker.Location.Y, marker.Location.Z,
					marker.Rotation.X, marker.Rotation.Y, marker.Rotation.Z,
					marker.Direction.X, marker.Direction.Y, marker.Direction.Z,
					marker.Scale.X, marker.Scale.Y, marker.Scale.Z,
					marker.Color.Red, marker.Color.Green, marker.Color.Blue, marker.IsVisible ? marker.Color.Alpha : 0,
					marker.BobUpDown, marker.FaceCamera, 2, marker.RotateWithHeading,
					marker.TextureDictionary, marker.TextureName, marker.DrawOnEnts);
				var markerLocation = ToCfxVector(marker.Location);
				var pedMarkerDistance = markerLocation.DistanceToSquared(pedPos);

				//Logger.Info($"{markerLocation.ToString()}, dist: {pedMarkerDistance}");
				//Logger.Info($"X-radius: {markerLocation.X * marker.Scale.X}. Y-radius: {markerLocation.Y * marker.Scale.Y}. Z-radius: {markerLocation.Z * marker.Scale.Z}");

				if (pedMarkerDistance < marker.Scale.X+1.5f && pedMarkerDistance < marker.Scale.Y + 1.5f && pedMarkerDistance <= marker.Scale.Z + 1.5f) {
					// Is inside marker, (How do we track if enter or leave?)
					if (!insideCurrentMarkers.Contains(marker)) {
						Logger.Info("Entered marker!!");
						insideCurrentMarkers.Add(marker);
						Comms.Event(MarkersEvents.MarkerEntered).ToClient().Emit(marker);
					}
				} else if (insideCurrentMarkers.Contains(marker)) {
					insideCurrentMarkers.Remove(marker);
					Comms.Event(MarkersEvents.MarkerLeft).ToClient().Emit(marker);
					Logger.Info("Left marker :/");
				}
			}

			foreach(var marker in insideCurrentMarkers) {
				// Check if the marker's button was clicked, while inside marker
				if (marker.IsClickable) {
					//DisplayHelpText("Click ~" + marker.GetHotkey().ControlNativeName + "~ to do something");
					if (marker.GetHotkey().IsJustPressed()) {
						Comms.Event(MarkersEvents.MarkerClicked).ToClient().Emit(marker);
					}
				}
			}
		}

		private float Vdist(Vector3 v1, Vector3 v2) {
			return API.Vdist(v1.X, v1.Y, v1.Z, v2.X, v2.Y, v2.Z);
		}

		private CitizenFX.Core.Vector3 ToCfxVector(NFive.SDK.Core.Models.Vector3 vector) {
			return new CitizenFX.Core.Vector3(vector.X, vector.Y, vector.Z);
		}

		private void DisplayHelpText(string text) {
			API.BeginTextCommandDisplayHelp("STRING");
			API.AddTextComponentAppTitle(text, -1);
			API.EndTextCommandDisplayHelp(0, false, true, -1);
		}
	}
}
