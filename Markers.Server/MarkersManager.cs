using Addemod.Markers.Server.Models;
using Addemod.Markers.Server.Storage;
using JetBrains.Annotations;
using NFive.SDK.Server.Communications;
using NFive.SDK.Server.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Addemod.Markers.Server {
	[Component(Lifetime = Lifetime.Singleton)]
	[PublicAPI]
	public class MarkersManager {
		private readonly ICommunicationManager comms;

		public MarkersManager(ICommunicationManager comms) {
			this.comms = comms;
		}

		public async Task AddMarker(Marker marker) {
			using (var ctx = new StorageContext()) {
				ctx.Markers.Add(marker);
				await ctx.SaveChangesAsync();
			}
		}

		public async Task RemoveMarker(Guid markerId) {
			using (var ctx = new StorageContext()) {
				var marker = ctx.Markers.First(m => m.Id == markerId);
				marker.Deleted = DateTime.UtcNow;
				await ctx.SaveChangesAsync();
			}
		}

		public List<Marker> GetAllMarkers(bool includeDeleted = false) {
			using(var ctx = new StorageContext()) {
				return ctx.Markers.Where(m => includeDeleted || m.Deleted.HasValue == false).ToList();
			}
		}
	}
}
