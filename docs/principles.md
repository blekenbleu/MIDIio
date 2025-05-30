[*back*](../README.md)

### blekenbleu.MIDIio principles of operation

Operation is controlled by [`NCalcScripts/MIDIio.ini`](../NCalcScripts/MIDIio.ini); 
  see [**Exporting property : [ExportProperty]**](https://github.com/SHWotever/SimHub/wiki/NCalc-scripting#exporting-event-trigger--exportevent).  
Search for `midi` in **SimHub Available properties**:

- `ExternalScript.MIDIout`:&nbsp;
   Case-sensitive name of [MIDI target](https://freevstplugins.net/category/midi-vst/controllers/)

- `ExternalScript.MIDIin`:&nbsp;
  Case-sensitive name of [MIDI source](https://en.wikipedia.org/wiki/MIDI_controller)

- `DataCorePlugin.ExternalScript.MIDIbuttons`, `MIDIknobs`, `MIDIsliders`:&nbsp;  
   These are *configuration arrays* of [MIDI CC message numbers](https://professionalcomposers.com/midi-cc-list/),  
   for which MIDIio generates respectively properties *in sequence*, e.g.:  
   `MIDIio.knob[0-n]`, `slider[0-n]`, `button[0-n]`,  *whether or not configured CC numbers are sequential*.  
   These properties track values received from those `MIDIin` CC numbers.  
   Each configured `button[0-n]` also generates a [**Source** (`Event`) for
   **SimHub Event mapping**](https://github.com/SHWotever/SimHub/wiki/NCalc-scripting#exporting-event-trigger--exportevent).  
   MIDIio handles each `MIDIin` message as received and routes properties configured for output in near-real-time.  

-  `ExternalScript.MIDIecho`:  
   `> 0`:&nbsp; forwards unconfigured `MIDIin` CC messages to `MIDIout` with no corresponding properties generated  
   `== 0`:&nbsp; dynamically generates properties `MIDIio.CC[0-127]` for unconfigured CC messages received.  
   These can be used to identify CC numbers for configuring [`NCalcScripts/MIDIio.ini`](../NCalcScripts/MIDIio.ini).  

-  `ExternalScript.MIDIsendCC` identify properties that become `MIDIout` messages to address CC.  
   If first 7 characters of those property names are 'MIDIio.', then those are among `MIDIin` CC properties  
   `MIDIio.knob[0-n]`, `slider[0-n]`, `button[0-n]`, or `CC[0-127]`, with matching `MIDIin` CC changes sent.  
	One may in theory assign `slider`, `button` or `knob` names for all 128 CC numbers.  

- Unlike `MIDIini` property names, destination property names are suffixed by device-specific address numbers  
  in *any* order, so long as addresses are valid.

- Unlike all other source and destination property names,  
  the lowest vJoy button is `button1` for device address `1`;&nbsp; other device type names may be suffixed `0`.

-  `MIDIio.ping[0-7]` are **SimHub Actions**
   to be associated with Triggers/Sources in
   [SimHub Event mapping](https://github.com/SHWotever/SimHub/wiki/NCalc-scripting#exporting-event-trigger--exportevent).  
   They share destination CC numbers with configured `MIDIsend` properties
   and are generated to help identify those SimHUb property CC numbers to a `MIDIout` target.  
   For example, if *both and only* `MIDIsend0` and `MIDIsend1` are configured for `MIDIio.*`,  
   then `ping0` and `ping1` SimHub Action **Target**s are generated, and mapping e.g. `CCn` with `ping1`  
   in **Mapping Picker** enables arbitrary messages to the second configured destination CC number by `MIDIin CCn`.

-  `ExternalScript.VJDbutton[0-7]` configure **[vJoy](https://github.com/blekenbleu/vJoySDK) button** changes from specified properties.  
   If configured names' first 7 characters are 'MIDIio.', then those properties should be among `MIDIio.button[0-n]`.  

-  `ExternalScript.VJDaxis[0-7]` specify properties for **[vJoy](https://github.com/blekenbleu/vJoySDK) axes** changes;&nbsp;  
    selecting among ShakeIt game properties, `MIDIio.slider[0-n]`, `MIDIio.knob[0-n]`, or `MIDIio.CC[0-127]`  
	sends their rescaled property changes as vJoy axes values.  

-  Each source property may get multiple send assignments,  
   but only one destination assignment on each output device for any single MIDIin property.  

**MIDI supports only 128 CC message numbers per channel;&nbsp; MIDIio supports only one device channel.**  
For example, configured `ExternalScript.MIDIsend[0-7]` CC messages  
may be mixed with *unconfigured* `MIDIin` messages with matching CC numbers  
when `MIDIecho` is configured to `'1'`.  

`MIDIio.*` properties are NEVER forwarded *by default* (`MIDIecho '0'`) or unless specifically configured.  

**When restarted, MIDIio in DoEcho `'1'` mode *resends* saved values for *unconfigured* `MIDIin.CC*` properties.**  
- Configured MIDIout CC messages from before restart are supposed inappropriate for a possibly different game.

- DoEcho `'1'` `MIDIin` CC messages *might* help [re]configure a `MIDIout` target device,  
  which may have also been restarted.

### Run time operation
SimHub's licensed update rate is 60Hz.  
**MIDIio** rescales and sends property change values among 3 destinations, provided those destinations are enabled:  
`MIDI out`, `vJoy axes`, `vJoy buttons`, where `MIDI out` may be slider, knob or button CCs.  
Any *destination* property may be configured from one of 6 *source* property types:  
`game telemetry` (most likely ShakeIt), `JoyStick axes`, `JoyStick buttons`, and `MIDIin` sliders+knobs+buttons.

**MIDIio** refreshes destinations for all but the first (i.e. non-game) source types even when a game is not running;  
game property changes are forwarded *only while games run*.  

To [minimize runtime overhead](Which.md), output (from `SendIf()`) is List driven:  
-  `SourceList[]` entries index ranges of `vJoy axis`,`vJoy button` and `MIDI CC` destination device addresses  
   for configured game, `JoyStick axis`, and `JoyStick button` source properties.  
-  `ListCC[]` entries similarly index MIDI source properties destined to addresses for those same destination device.

`SourceArray[,]` and `CCarray[,]` respectively correspond to `SendName[,]` and `CCname[]` source names, where:  
`CCname[]` has 128 entries for each possible `MIDIin` property, whether or not configured  
`SourceList.Length` is `SourceType.Lenght - 1`, (omitting CC type)  
for configured game, JoyStick axis, JoyStick button property Listss.  

Property Lists configured for any source to any destination are variable,  
allowing for *relatively* low pain addition of sources and destinations. 
