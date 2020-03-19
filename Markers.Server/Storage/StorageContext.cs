using System.Data.Entity;
using Addemod.Markers.Server.Models;
using NFive.SDK.Core.Models.Player;
using NFive.SDK.Server.Storage;

namespace Addemod.Markers.Server.Storage
{
	public class StorageContext : EFContext<StorageContext>
	{
		public DbSet<Marker> Markers { get; set; }
	}
}
