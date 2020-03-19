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
		
		public MarkersService(ILogger logger, ITickManager ticks, ICommunicationManager comms, ICommandManager commands, IOverlayManager overlay, User user) : base(logger, ticks, comms, commands, overlay, user) { }

		public override async Task Started()
		{
			// Request server configuration
			this.config = await this.Comms.Event(MarkersEvents.Configuration).ToServer().Request<Configuration>();
			// Listen for reload event
			this.Comms.Event(MarkersEvents.Reload).FromServer().On(Reload);

			Setup();
		}

		private async void Setup(bool reload = false) {
			if(reload)
				this.Ticks.Off(MarkersTick);

			this.config = await this.Comms.Event(MarkersEvents.Configuration).ToServer().Request<Configuration>();
			this.markers = await this.Comms.Event(MarkersEvents.GetAllMarkers).ToServer().Request<List<Marker>>();
			
			this.Ticks.On(MarkersTick);
		}

		public void Reload(ICommunicationMessage e) => Setup(true);

		/// <summary>
		/// Returns all markers that the server sent to us upon load
		/// </summary>
		/// <returns>List<Marker></returns>
		public List<Marker> GetAllMarkers() {
			return this.markers;
		}

		/// <summary>
		/// Returns a list of all the markers the player currently is inside
		/// </summary>
		/// <returns>List<Marker></returns>
		public List<Marker> GetInsideMarkersList() {
			return this.markers.Where(m => m.HasPlayerInside == true).ToList();
		}

		/// <summary>
		/// Get the marker that the player is inside, if inside multiple markers, the closests one is returned.
		/// </summary>
		/// <returns>Marker</returns>
		public Marker GetInsideClosestMarker() {
			var pedPos = Game.PlayerPed.Position;

			// Wtf should I call it?? Of all markers that you are inside, get the closests one
			Marker closestMarkerInside = null;
			foreach (var marker in this.GetInsideMarkersList()) {
				var markerLocation = ToCfxVector(marker.Location);
				var pedMarkerDistance = markerLocation.DistanceToSquared(pedPos);
				if (closestMarkerInside == null) {
					closestMarkerInside = marker;
					continue;
				} else {
					// If the currently closest marker is further away from the marker we are checking right now
					if (ToCfxVector(closestMarkerInside.Location).DistanceToSquared(pedPos) > pedMarkerDistance) {
						// Set the closest marker to the new one
						closestMarkerInside = marker;
					}
				}
			}
			return closestMarkerInside;
		}

		private void MarkersTick() {
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

				// Check if inside marker
				if (pedMarkerDistance < marker.Scale.X+1.5f && pedMarkerDistance < marker.Scale.Y + 1.5f && pedMarkerDistance <= marker.Scale.Z + 1.5f) {
					// Check if the player has entered the marker already
					if (!marker.HasPlayerInside) {
						// If not, set as is inside
						marker.HasPlayerInside = true;
						// Emit the event
						Comms.Event(MarkersEvents.MarkerEntered).ToClient().Emit(marker);
					}
				} else if (marker.HasPlayerInside) {
					// If not inside the marker anymore, but the marker previously had the player inside
					// set as not inside
					marker.HasPlayerInside = false;
					// Emit the event
					Comms.Event(MarkersEvents.MarkerLeft).ToClient().Emit(marker);
				}
			}

			foreach(var marker in this.GetInsideMarkersList()) {
				// Check if the marker's key was clicked while inside marker
				if (marker.IsClickable && marker.Hotkey.IsJustPressed()) {
					Logger.Info("Pressed " + marker.Hotkey.ControlNativeName);
					Comms.Event(MarkersEvents.MarkerClicked).ToClient().Emit(marker);
				}
			}
		}

		private CitizenFX.Core.Vector3 ToCfxVector(NFive.SDK.Core.Models.Vector3 vector) {
			return new CitizenFX.Core.Vector3(vector.X, vector.Y, vector.Z);
		}

		/*private async void AddMarkerCommand(List<string> args) {
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
		}*/

		/*private async void RemoveMarkerCommand() {
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
		}*/
	}
}
