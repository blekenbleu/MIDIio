[*back*](../../../)

### blekenbleu.MIDIio principles of operation

Operation is controlled by [`NCalcScripts/MIDIio.ini`](../NCalcScripts/MIDIio.ini):  
Search for `midi` in **SimHub Available properties**:

- `ExternalScript.MIDIout`:
   Case-sensitive name of MIDI receiver

- `ExternalScript.MIDIin`:
  Case-sensitive name of MIDI source

- `DataCorePlugin.ExternalScript.MIDIbuttons`, `MIDIknobs`, `MIDIsliders`:  
   MIDI CC message number configuration arrays,
   for which MIDiio generates respectively:

-  properties `MIDIio.knob[0-7]`, `slider[0-7]`, `button[0-7]`
   values track those from configured `MIDIin` CC numbers

-  `ExternalScript.MIDIecho`:  
   `> 0`: forwards unconfigured MIIDIin CC messages to MIDIout  
   `== 0`: dynamically adds properties for unconfigured CC message numbers

-  `ExternalScript.MIDIsend[0-7]` define properties for which value changes
   become CC messages to MIDIout.  
   If first 7 characters of those property names are 'MIDIio.', then properties are `MIDIin messages`  
   from e.g. `MIDIio.knob[0-7]`, `slider[0-7]`, `button[0-7]` with sent messages matching received CCs.

MIDI supports only 127 CC message numbers per channel.  
For example, if `ExternalScript.MIDIsend[0-7]` are configured  
with no `MIDIio.knob[0-7]`, `slider[0-7]`, `button[0-7]` defined,  
then MIDIio assigns CC[0-7] message numbers for `MIDIsend[0-7]` property value changes to `MIDIout`,  
which will conflict with CC[0-7] messages forwarded from `MIDIin` when `0 < MIDIecho`.    
This *might* allow assigning MIDI device controls for superseding some SimHub property messages...

Since configured `MIDIio.*` properties are NOT forwarded by default,  
MIDIio maps `MIDIsend[0-7]` properties not configured for MIDIio.  
 to use CC numbers from first 8 configured `MIDIin.*` properties.

If/when e.g. a MIDIsend0 definition is removed from `NCalcScripts/MIDIio.ini`  
then other configured `MIDIsend1-7]` properties not set to `MIDIio.*` will send *changed* CC numbers to MIDIout.

When restarted by SimHub, MIDIio resends saved values for configured MIDIin.* properties,  
but NOT from SimHub properties, e.g. `ShakeITBSV3Plugin.Export.*`
* properties from a game before restart may be inappropriate for a possibly different newly started game.
* properties for configured `MIDIin` numbers may be used for configuring `MIDIout` receiver,  
  which may have also been restarted.

