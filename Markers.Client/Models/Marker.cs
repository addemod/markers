using Addemod.Markers.Shared.Models;
using CitizenFX.Core;
using NFive.SDK.Client.Input;
using NFive.SDK.Core.Input;
using NFive.SDK.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3 = NFive.SDK.Core.Models.Vector3;

namespace Addemod.Markers.Client.Models {
	public class Marker : IdentityModel, IMarker {
		public Vector3 Location { get; set; }
		public Vector3 Rotation { get; set; }
		public Vector3 Direction { get; set; }
		public Vector3 Scale { get; set; }
		public RGBColor Color { get; set; }
		public int MarkerType { get; set; }
		public bool FaceCamera { get; set; }
		public bool BobUpDown { get; set; }
		public bool DrawOnEnts { get; set; }
		public bool RotateWithHeading { get; set; }
		public bool IsVisible { get; set; }
		public string TextureDictionary { get; set; }
		public string TextureName { get; set; }
		public bool IsClickable { get; set; }
		public InputControl ClickInputControl { get; set; }

		private Hotkey _hotKey;

		public MarkerType GetMarkerType() {
			return (MarkerType)this.MarkerType;
		}

		public Hotkey GetHotkey() {
			_hotKey = _hotKey ?? new Hotkey(this.ClickInputControl);
			return _hotKey;
		}

		public Marker() {
			this.Location = new Vector3();
			this.Direction = new Vector3();
			this.Scale = new Vector3(3, 3, 1);
			this.Rotation = new Vector3();
			this.Color = new RGBColor();
			this.MarkerType = 1;
			this.FaceCamera = false;
			this.BobUpDown = false;
			this.IsVisible = true;
			this.RotateWithHeading = false;
			this.DrawOnEnts = false;
			this.TextureDictionary = null;
			this.TextureName = null;
			this.IsClickable = true;
			this.ClickInputControl = InputControl.PhoneSelect;
		}
	}
}
