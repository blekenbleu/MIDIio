[*back*](../README.md)

## blekenbleu.MIDIio principles of operation

[`NCalcScripts/MIDIio.ini`](../NCalcScripts/MIDIio.ini) configures operation;&nbsp; 
  see [**Exporting property : [ExportProperty]**](https://github.com/SHWotever/SimHub/wiki/NCalc-scripting#exporting-event-trigger--exportevent).  
Search for `midi` in **SimHub Available properties**:

- `ExternalScript.MIDIout`:&nbsp;
   Case-sensitive [MIDI target](https://freevstplugins.net/category/midi-vst/controllers/) name  

- `ExternalScript.MIDIin`:&nbsp;
  Case-sensitive [MIDI source](https://en.wikipedia.org/wiki/MIDI_controller) name 

- `DataCorePlugin.ExternalScript.MIDIbuttons`, `MIDIknobs`, `MIDIsliders`:&nbsp;  
   These are [MIDI CC message numbers](https://professionalcomposers.com/midi-cc-list/) *configuration arrays*,  
   for which MIDIio respectively generates properties *in defined sequence*, e.g.:  
   `MIDIio.knob[0-n]`, `slider[0-n]`, `button[0-n]`,  *whether or not configured CC numbers are sequential*.  
   `MIDIin` CC number received events update these properties.  
   `MIDIsends` [Event Triggers](https://github.com/SHWotever/SimHub/wiki/NCalc-scripting#exporting-event-trigger--exportevent)
   are implemented in C# for property *value* (not just `true/false`) changes.  
   MIDIio handles Non-CC configured SimHub (e.g. joystick) properties (@ 60Hz) in `MIDIio.DataUpdate()`.  
   MIDIio processes `MIDIin` CC messages on receipt, routing changed values in near-real-time.  
	- One may in theory assign `slider`, `button` or `knob` names for all 128 CC numbers.  


-  `ExternalScript.MIDIecho`:  
   `> 0`:&nbsp; forwards unconfigured `MIDIin` CC messages to `MIDIout`;  
   properties `MIDIio.CC[0-127]` are dynamically generated for unconfigured CC messages as received.  
   These can be used to identify CC numbers for configuring [`NCalcScripts/MIDIio.ini`](../NCalcScripts/MIDIio.ini).  

### [MIDIio Events and Actions](Which.md)
-  [`ExternalScript.MIDIsends`](sends.md) enumerates **up to 8** vJoy or `MIDIout` CC SimHub **Action** message destinations.  
	MIDIio also generates **Event Triggers** for configured source property changes.  
	Destinations are prefixed by either **A**, **B**, or **C**, appended by a `vJoy Axis`, `vJoy Button` or `CC address number`.  
	For example, `A0` would be the first vJoy axis, `B1` would be the first vJoy button, `C55` would be CC `55`.  
   First 7 characters of corresponding property names being 'MIDIio.' are **assumed** `MIDIin` CC property values  
   `MIDIio.knob[0-n]`, `slider[0-n]`, `button[0-n]`, or `CC[0-127]`, with respective `MIDIin` CC changed values sent.  
	- Unlike sequentially-asssigned source names,  
		`MIDIsend` names are suffixed by valid device-specific address numbers.  

- The first possible `MIDIsend` vJoy button is `B1`;&nbsp; first `MIDIsend` vJoy axis and destination CC are `A0` and `C0`.

-  `MIDIio.send[0-nn]` are **SimHub Actions** for SimHub Triggers/Sources in
   [SimHub Event mapping](https://github.com/SHWotever/SimHub/wiki/NCalc-scripting#exporting-event-trigger--exportevent).  
   Their destination addresses are configured from `MIDIsends`, passing values from configured SimHUb sources.  
   For example, if *both and only* `MIDIsend0` and `MIDIsend1` are configured for `MIDIio.*`,  
   then `send0` and `send1` SimHub Action **Target**s are generated.  
   SimHub **Mapping Picker** enables arbitrary Event to Action assignments,  
	but MIDIio Action property *value* associations are set in `MIDIio.ini`.

-  `ExternalScript.sendB[n]` configure **[vJoy](https://github.com/blekenbleu/vJoySDK) button** changes from specified properties.  

-  `ExternalScript.sendA[n]` specify properties for **[vJoy](https://github.com/blekenbleu/vJoySDK) axes** changes;&nbsp;  
    selecting among ShakeIt, game, joystick, `MIDIio.slider[0-n]`, `MIDIio.knob[0-n]`, or `MIDIio.CC[0-127]`  
	sends those rescaled property changes as vJoy axes values.  

-  `MIDIio.ini` supports each `MIDIin` assignment to no more than one address  
	for each of vJoy axis, vJoy button, and `MIDIout`,
   for a maximum of 3 routings, one per output device.  
   However, each `MIDIin` source can also be in a single `MIDIsends` assignment..  

-  `MIDIio.ini MIDIsends` configuration cannot duplicate source and destination Action and Event assignments.  
	Of course, SimHub allows for multiple mappings among Events and Actions. 

**MIDI supports only 128 CC message numbers per channel;&nbsp; MIDIio supports only one channel.**  
For example, configured `ExternalScript.MIDIsend[0-7]` CC messages  
may be mixed with *unconfigured* `MIDIin` messages with matching CC numbers  
when `MIDIecho` is configured to `'1'`.  

`MIDIio.*` properties are NOT forwarded for `MIDIecho '0' unless specifically configured.  

**When restarted, MIDIio in DoEcho `'1'` mode *resends* saved values for *unconfigured* `MIDIin.CC*` properties.**  
- Messages from *configured* MIDIin before restart are NOT resent.
- DoEcho `'1'` unconfigured CC messages *might* help [re]configure the `MIDIout` target device,  
  which may have also been restarted.
- DoEcho `'0'` unconfigured CC properties are not preserved across restarts


### Run time operation
SimHub's licensed update rate is 60Hz.  
**MIDIio** rescales and sends property change values among 3 destinations, provided those destinations are enabled:  
`MIDI out`, `vJoy axes`, `vJoy buttons`, where `MIDI out` may be slider, knob or button CCs.  
Any *destination* property may be configured from one of 6 *source* property types:  
`game telemetry` (e.g. ShakeIt or JSONio), `JoyStick axes`, `JoyStick buttons`, and `MIDIin` sliders+knobs+buttons.

**MIDIio** refreshes destinations for all but the first (i.e. non-game) source types even when a game is not running;  
game property changes are forwarded *only while games (or replays)  run*.  

To [minimize runtime overhead](Which.md), output (from `SendIf()`) is List driven:  
-  `SourceList[]` entries index ranges of `vJoy axis`,`vJoy button` and `MIDI CC` destination device addresses  
   for configured game, `JoyStick axis`, and `JoyStick button` source properties.  
-  `ListCC[]` entries similarly index MIDI source properties destined to addresses for those same destination device.

`SourceList[]` and `ListCC[]` respectively correspond to `SendName[,]` and `CCname[]` source names, where:  
`CCname[]` has 128 entries for each possible `MIDIin` property, whether or not configured  
`SourceList.Length` is `SourceType.Length - 1`, (omitting CC type)  
for configured game, JoyStick axis, JoyStick button property Lists.  

Property Lists configured for any source to any destination are variable,  
allowing for *relatively* low pain addition of sources and destinations. 

### *v0.0.2.6* adds vJoy destinations to [`MIDIio.ini 'MIDIsends'`](sends.md) events and actions  
