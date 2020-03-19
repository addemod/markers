# Addemod/markers
[![License](https://img.shields.io/github/license/Addemod/markers.svg)](LICENSE)
[![Build Status](https://img.shields.io/appveyor/ci/Addemod/markers/master.svg)](https://ci.appveyor.com/project/Addemod/markers)
[![Release Version](https://img.shields.io/github/release/Addemod/markers/all.svg)](https://github.com/Addemod/markers/releases)

A plugin to simplify marker usage in NFive plugin development.

Please note that this is not a plugin that you solely use alone. Its for plugin developers that want to simplify their development process when creating markers.

## Installation

Install the plugin into your server from the [NFive Hub](https://hub.nfive.io/Addemod/markers):
```
nfpm install Addemod/markers
```

## Usage

```csharp
private MarkersManager markerManager;

public MyPluginController(ILogger logger, Configuration configuration, ICommunicationManager comms, MarkersManager markerManager): base(logger, configuration)
{
	this.markerManager = markerManager;

	// Send configuration when requested
	comms.Event(MyPluginEvents.Configuration).FromClients().OnRequest(e => e.Reply(this.Configuration));
	}
}
```

## Events
### Client
#### MarkerEntered
Triggered when a player entered a marker
```csharp
void MarkerEnteredEvent(ICommunicationMessage e, Marker marker) { /*  Do stuff */ }
```
#### MarkerLeft
Triggered when a player left a marker
```csharp
void MarkerLeftEvent(ICommunicationMessage e, Marker marker) { /*  Do stuff */ }
```
#### MarkerClicked
Triggered if a marker has "clicking" enabled, and the player clicked the defined key for this marker
```csharp
void MarkerClickedEvent(ICommunicationMessage e, Marker marker) { /*  Do stuff */ }
```
