using NFive.SDK.Core.Input;
using NFive.SDK.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Addemod.Markers.Shared.Models {
	public interface IMarker : IIdentityModel {
		Vector3 Location { get; set; }
		Vector3 Rotation { get; set; }
		Vector3 Direction { get; set; }
		Vector3 Scale { get; set; }
		RGBColor Color { get; set; }
		bool RotateWithHeading { get; set; }
		int MarkerType { get; set; }
		string TextureDictionary { get; set; }
		string TextureName { get; set; }
		bool FaceCamera { get; set; }
		bool BobUpDown { get; set; }
		bool DrawOnEnts { get; set; }
		bool IsClickable { get; set; }
		bool IsVisible { get; set; }
		InputControl ClickInputControl { get; set; }
	}
}
