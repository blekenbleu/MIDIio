[*back*](../../../)

### blekenbleu.MIDIio principles of operation

Operation is controlled by [`NCalcScripts/MIDIio.ini`](../NCalcScripts/MIDIio.ini); 
  see [**Exporting property : [ExportProperty]**](https://github.com/SHWotever/SimHub/wiki/NCalc-scripting#exporting-event-trigger--exportevent).  
Search for `midi` in **SimHub Available properties**:

- `ExternalScript.MIDIout`:&nbsp;
   Case-sensitive name of [MIDI receiver](https://freevstplugins.net/category/midi-vst/controllers/)

- `ExternalScript.MIDIin`:&nbsp;
  Case-sensitive name of [MIDI source](https://en.wikipedia.org/wiki/MIDI_controller)

- `DataCorePlugin.ExternalScript.MIDIbuttons`, `MIDIknobs`, `MIDIsliders`:  
   [MIDI CC message number](https://professionalcomposers.com/midi-cc-list/) configuration arrays,
   for which MIDiio generates respectively:

   -  properties `MIDIio.knob[0-7]`, `slider[0-7]`, `button[0-7]`,  
      which track values received from configured `MIDIin` CC numbers.  
      Each configured `button[0-7]` also generates a [**Source** (`Event`) for **SimHub Event mapping**](https://github.com/SHWotever/SimHub/wiki/NCalc-scripting#exporting-event-trigger--exportevent).

-  `ExternalScript.MIDIecho`:  
   `> 0`:&nbsp; forwards unconfigured `MIDIin` CC messages to `MIDIout` with no corresponding properties generated  
   `== 0`:&nbsp; dynamically generates:

    -  properties `MIDIio.CC[0-127]` for unconfigured `MIDIin` CC messages received  
       with numbers not specified in `MIDIio.knob[0-7]`, `slider[0-7]` or `button[0-7]`  
       when `ExternalScript.MIDIecho == 0`.&nbsp; These can be used for discovering CC numbers to configure.

-  `ExternalScript.MIDIsend[0-7]` configure properties for which value changes
   become CC messages to `MIDIout`.  
   If first 7 characters of those property names are 'MIDIio.', then those properties are `MIDIin` messages  
   from e.g. `MIDIio.knob[0-7]`, `slider[0-7]`, `button[0-7]`, `CC[0-127]` with sent messages matching received CCs.

-  `MIDIio.ping[0-7]` are **SimHub Actions**
   to be associated with Triggers/Sources in
 [SimHub Event mapping](https://github.com/SHWotever/SimHub/wiki/NCalc-scripting#exporting-event-trigger--exportevent).  
   They share CC numbers with configured `MIDIsend` properties that are NOT 'MIDIio.*',  
   and are generated to help identify those SimHUb property CC numbers on a `MIDIout` receiver.  
   For example, if *both and only* `MIDIsend0` and `MIDIsend1` are configured for `MIDIio.*`,  
 then e.g.  configured `MIDIsend2` will cause `ping2` SimHub Action **Target** to be generated, and  
 mapping e.g. `CCn` with `ping2` in **Mapping Picker** enables arbitrary messages using `MIDIsend2`'s CC number.

**MIDI supports only 127 CC message numbers per channel.**  
For example, if `ExternalScript.MIDIsend[0-7]` are configured  
*while no* `MIDIio.knob[0-7]`, `slider[0-7]`, `button[0-7]` are configured,  
then MIDIio assigns CC[0-7] message numbers for `MIDIsend[0-7]` property value changes to `MIDIout`.  
For `MIDIecho > 0` in this exampe, `MIDIsend[0-7]` would mix with unconfigured CC[0-7] messages from `MIDIin`,  
but `ping[0-7]` are specifically implemented to enable configuring any `MIDIin CCn` Events for mixing.

Since configured `MIDIio.*` properties are NOT forwarded by default,  
MIDIio maps `MIDIsend[0-7]` properties *not* configured for `MIDIio.*`  
 to use CC numbers from first configured `MIDIin.*` properties.  
Anytime *non*-`MIDIio.*` properties configured as `MIDIsend[0-7] >` configured `MIDIin` properties,  
excess `MIDIsend[0-7]` properties will appropriate lowest unconfigured CC numbers, risking potential mixing.

If/when e.g. a MIDIsend0 definition is removed from `NCalcScripts/MIDIio.ini`  
then other configured `MIDIsend1-7]` properties not set to `MIDIio.*` will send *changed* CC numbers to `MIDIout`.

When restarted by SimHub, MIDIio resends saved values for configured `MIDIin.*` properties,  
but NOT from SimHub properties, e.g. `ShakeITBSV3Plugin.Export.*`
* properties from a game before restart may be inappropriate for a possibly different newly started game.
* properties for configured `MIDIin` numbers may be used for configuring `MIDIout` receiver,  
  which may have also been restarted.

