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
   These are *configuration arrays* of up to 8 each [MIDI CC message numbers](https://professionalcomposers.com/midi-cc-list/),  
   &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; for which MIDIio generates respectively properties:&nbsp;
   `MIDIio.knob[0-7]`, `slider[0-7]`, `button[0-7]`,  
   &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; which track values received from those `MIDIin` CC numbers.  
   Each configured `button[0-7]` also generates a [**Source** (`Event`) for
   **SimHub Event mapping**](https://github.com/SHWotever/SimHub/wiki/NCalc-scripting#exporting-event-trigger--exportevent).

-  `ExternalScript.MIDIecho`:  
   `> 0`:&nbsp; forwards unconfigured `MIDIin` CC messages to `MIDIout` with no corresponding properties generated  
   `== 0`:&nbsp; dynamically generates properties `MIDIio.CC[0-127]` for unconfigured CC messages received.  
   These can be used to identify CC numbers for configuring [`NCalcScripts/MIDIio.ini`](../NCalcScripts/MIDIio.ini).
-  `ExternalScript.MIDIsend[0-7]` identify properties for which value changes become CC messages to `MIDIout`.  
   If first 7 characters of those property names are 'MIDIio.', then those are among `MIDIin` CC properties
   `MIDIio.knob[0-7]`, `slider[0-7]`, `button[0-7]`, or `CC[0-127]`, with matching `MIDIin` CC changes sent.

-  `MIDIio.ping[0-7]` are **SimHub Actions**
   to be associated with Triggers/Sources in
   [SimHub Event mapping](https://github.com/SHWotever/SimHub/wiki/NCalc-scripting#exporting-event-trigger--exportevent).  
   They share CC numbers with configured `MIDIsend` properties that are NOT 'MIDIio.*',  
   and are generated to help identify those SimHUb property CC numbers to a `MIDIout` target.  
   For example, if *both and only* `MIDIsend0` and `MIDIsend1` are configured for `MIDIio.*`,  
 then e.g.  configured `MIDIsend2` will cause `ping2` SimHub Action **Target** to be generated, and  
 mapping e.g. `CCn` with `ping2` in **Mapping Picker** enables arbitrary messages using `MIDIsend2`'s CC number.

-  `ExternalScript.VJDbutton[0-7]` configure **[vJoy](https://github.com/blekenbleu/vJoySDK) button** changes from specified properties.  
   If configured names' first 7 characters are 'MIDIio.', then those properties should be among `MIDIio.button[0-7]`.  

-  `ExternalScript.VJDaxis[0-7]` specify properties for **[vJoy](https://github.com/blekenbleu/vJoySDK) axes** changes;&nbsp; selecting among  
   `MIDIio.slider[0-7]`, `MIDIio.knob[0-7]`, or `MIDIio.CC[0-127]` sends their rescaled property changes as vJoy axes values.  

-  Each property may get multiple send assignments.  

**MIDI supports only 127 CC message numbers per channel.**  
For example, if `ExternalScript.MIDIsend[0-7]` are configured  
*while no* `MIDIio.knob[0-7]`, `slider[0-7]`, `button[0-7]` are configured,  
then MIDIio assigns CC[0-7] message numbers for `MIDIsend[0-7]` property value changes to `MIDIout`.  
For `MIDIecho > 0` in this exampe, `MIDIsend[0-7]` would mix with unconfigured CC[0-7] messages from `MIDIin`,  
but `ping[0-7]` are specifically implemented to enable configuring any `MIDIin CCn` Events for mixing.

Since configured `MIDIio.*` properties are NOT forwarded by default,  
MIDIio may remap `MIDIsend[0-7]` properties *not* configured for `MIDIio.*`  
 to use CC numbers from `MIDIin.*` properties *not* configured for `MIDIout`.  
Anytime *non*-`MIDIio.*` properties configured as `MIDIsend[0-7] >` configured `MIDIin` properties,  
excess `MIDIsend[0-7]` properties will appropriate highest unconfigured CC numbers, risking potential mixing.

If/when e.g. a `MIDIsend` definition is removed from `NCalcScripts/MIDIio.ini`  
then other configured `MIDIsend` properties not set to `MIDIio.*` may send *changed* CC numbers to `MIDIout`.

When restarted by SimHub, MIDIio in DoEcho mode resends values for unconfigured `MIDIin.*` properties,  
but NOT from SimHub properties, e.g. `ShakeITBSV3Plugin.Export.*`
* properties from a game before restart may be inappropriate for a possibly different newly started game.
* DoEcho'ed `MIDIin` CC messages *might* help [re]configure a `MIDIout` target,  
  which may have also been restarted.

### Run time operation
SimHub's licensed update rate is 60Hz.&nbsp;  The **MIDIio** plugin handles *only its configured* properties.  
**MIDIio** rescales and sends property change values among 3 destinations, provided those destinations are enabled:  
`MIDI out`, `vJoy axes`, `vJoy buttons`, where `MIDI out` may be slider, knob or button CCs.  
Any *destination* property may be configured from one of 4 *source* property types:  
`MIDIin`, `JoySick axes`, `JoyStick buttons`, and `game telemetry` (most likely ShakeIt).  
**MIDIio** refreshes destinations for those first 3 (hardware) source types even when games are not running;  
game property changes are forwarded *only while games run*.  

To minimize runtime overhead, output (from `DoSend()`) is table driven:  
-  2 `table[][]` entries index ranges of `table[][]` indices to send with games running [1] or anytime [0]
-  3 `table[][]` entries index `Map[][]` ranges of `MIDI in`, `JoyStick axis`, `JoyStick button` destinations  
   for configured `MIDI in`, `JoySick axis`, and `JoyStick button` sources.
-  the final table entry indexes `Map[][]` range of game (`ShakeIt`) property sources.

Those `table[][]` entries index into a `Map[][]` array, where:  
- one `Map` dimension indexes one `Send[][]` source property array dimension per destination type  
- another `Map` dimension indexes another `Send[][]` array dimension  
  for each configured property change for that indexed destination type.

`Map[][]` and `Send[][]` are so-called [jagged arrays](https://www.programiz.com/csharp-programming/jagged-array).  
Property counts for any source to any destination are variable, only *total* counts are constrained.  
These table indirections support sending from none up to the configured maximum number of properties  
from any source to any destination.&nbsp; The `table[][]` array has fixed dimensions:   
- index range limit pairs for each of 4 sources, presorted  
- index range limit pairs for game vs non-game sources, presorted  

This arrangement should also allow for relatively low pain extension to additional sources and destinations. 
