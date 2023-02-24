[*back*](../../../)

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

-  `ExternalScript.MIDIecho`:  
   `> 0`:&nbsp; forwards unconfigured `MIDIin` CC messages to `MIDIout` with no corresponding properties generated  
   `== 0`:&nbsp; dynamically generates properties `MIDIio.CC[0-127]` for unconfigured CC messages received.  
   These can be used to identify CC numbers for configuring [`NCalcScripts/MIDIio.ini`](../NCalcScripts/MIDIio.ini).  
-  `ExternalScript.MIDIsend[0-7]` identify up to `size` properties that become `MIDIout` CC messages.  
   If first 7 characters of those property names are 'MIDIio.', then those are among `MIDIin` CC properties  
   `MIDIio.knob[0-n]`, `slider[0-n]`, `button[0-n]`, or `CC[0-127]`, with matching `MIDIin` CC changes sent.  
	One may in theory assign `slider`, `button` or `knob` names for all 128 CC numbers.  
	Other source and destinations are constrained by the `MIDIsize` parameter.  

- Somewhat like Source property arrays, destination property names will be sequenced,  
  but integers in destination arrays are *device addresses* in *any* order, so long as addresses are valid.

- Unlike all other source and destination property names,  
  the first vJoy button is `button1` and should be integer `1`;&nbsp; all other device type firsts are `0`.

-  `MIDIio.ping[0-7]` are **SimHub Actions**
   to be associated with Triggers/Sources in
   [SimHub Event mapping](https://github.com/SHWotever/SimHub/wiki/NCalc-scripting#exporting-event-trigger--exportevent).  
   They share CC numbers with configured `MIDIsend` properties that are NOT 'MIDIio.*',  
   and are generated to help identify those SimHUb property CC numbers to a `MIDIout` target.  
   For example, if *both and only* `MIDIsend0` and `MIDIsend1` are configured for `MIDIio.*`,  
 then e.g.  configured `MIDIsend2` will cause `ping2` SimHub Action **Target** to be generated, and  
 mapping e.g. `CCn` with `ping2` in **Mapping Picker** enables arbitrary messages using `MIDIsend2`'s CC number.

-  `ExternalScript.VJDbutton[0-7]` configure up to `size` **[vJoy](https://github.com/blekenbleu/vJoySDK) button** changes from specified properties.  
   If configured names' first 7 characters are 'MIDIio.', then those properties should be among `MIDIio.button[0-n]`.  

-  `ExternalScript.VJDaxis[0-7]` specify up to `size` properties for **[vJoy](https://github.com/blekenbleu/vJoySDK) axes** changes;&nbsp; selecting among  
   ShakeIt game properties, `MIDIio.slider[0-n]`, `MIDIio.knob[0-n]`, or `MIDIio.CC[0-127]` sends their rescaled property changes as vJoy axes values.  

-  Each source property may get multiple send assignments, but only one destination assignment on each output device for any single MIDIin property.  

**MIDI supports only 128 CC message numbers per channel;&nbsp; MIDIio supports only one device channel.**  
For example, configured `ExternalScript.MIDIsend[0-7]` CC messages  
may be mixed with *unconfigured* `MIDIin` messages with matching CC numbers when `MIDIecho` is configured to `'0'`.  

Configured `MIDIio.*` properties are NEVER forwarded *by default*.  

**When restarted, MIDIio in DoEcho `'1'` mode *resends* saved values for *unconfigured* `MIDIin.CC*` properties.**  
* Configured MIDIout CC messages from before restart are supposed inappropriate for a possibly different game.  
* DoEcho `'1'` `MIDIin` CC messages *might* help [re]configure a `MIDIout` target device,  
  which may have also been restarted.

### Run time operation
SimHub's licensed update rate is 60Hz.  
**MIDIio** rescales and sends property change values among 3 destinations, provided those destinations are enabled:  
`MIDI out`, `vJoy axes`, `vJoy buttons`, where `MIDI out` may be slider, knob or button CCs.  
Any *destination* property may be configured from one of 6 *source* property types:  
`game telemetry` (most likely ShakeIt), `JoyStick axes`, `JoyStick buttons`, and `MIDIin` sliders+knobs+buttons.

**MIDIio** refreshes destinations for all but the first (i.e. non-game) source types even when a game is not running;  
game property changes are forwarded *only while games run*.  

To minimize runtime overhead, output (from `DoSend()`) is table driven:  
-  2 `table[][]` entries index ranges of `SourceType` indices to send with games running [0] or anytime [1]  
-  `SourceArray[,]` entries index ranges of `vJoy axis` and `vJoy button` destinations  
   for configured game, `JoyStick axis`, and `JoyStick button` source properties.  
-  `CCarray[,]` entries index source properties destined to `MIDIout`.

`SourceArray[,]` and `CCarray[,]` entries respectively index into `SendName[,]` and `CCname[]` array for source properties, where:  
`CCname[]` is 128 entries for each possible MIDIin property, whether or not configured  
`SourceArray[,]` is a `SourceType - 1` by configured `size` array for configured game, `JoyStick axis`, `JoyStick button` properties.  

SourceCt[4] is a count array for each SourceType of configured properties.  
- each non-CC SendCt[0-2] value is limited to configured `size`.  
- SendCt[0] counts game property names in SourceName[0] array  
- SendCt[1-2] counts JoyStick axis, button property names in SourceName[1-2]  
- SendCt[3] counts MIDIin sliders, knobs, buttons configured in CCname[SourceArray[3,1,]]  
- Up to configured `3 * size` entries may be configured for MIDIin sliders, knobs, buttons.  

Property counts configured for any source to any destination are variable, only *total* counts are constrained by `size`.  
These table indirections support sending from none up to configured maximum property counts  
 &nbsp; from any source to any destination.&nbsp; The `table[,]` array has fixed dimensions:   
- table[0] indexes game properties;&nbsp; table[1] indexes non-game properties

This arrangement should also allow for *relatively* low pain extension to additional sources and destinations. 
