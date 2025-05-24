### blekenbleu.MIDIio SimHub plugin source code files (C# classes)
- [MIDIio.cs](../MIDIio.cs) class is the SimHub plugin equivalent of main().   
  It interfaces other classes to SimHub, handling properties, events, actions, initializations and cleanups.  
  - `Active()` updates **MIDIio** properties based on `INdrywet` ControlChangeEvents  
  - `DataUpdate()` invokes `DoSend()`.
  - `DoSend()` invokes `Send()` for writing changed properties to `VJsend` or `OUTdrywet`.   
- [MIDIioSettings.cs](../MIDIioSettings.cs) is *only* data to be saved and restored between plugin launches.  
- [CCProperties.cs](../CCProperties.cs) initializes properties for MIDIio.cs.  
- [INdrywet.cs](../INdrywet.cs) handles MIDI messages from `MIDIin`
  using [Melanchall.DryWetMidi](https://github.com/melanchall/drywetmidi)  
- [OUTdrywet.cs](../OUTdrywet.cs) sends MIDI messages to `MIDIout`.  
- [VJsend.cs](../VJsend.cs) sends button and axis values to a single vJoy device.
- [VJoyFFBReceiver.cs](../VJoyFFBReceiver.cs) placeholder code for handling vJoy force feedback data.

### MIDIio.ini
- [MIDIio.ini](../NCalcScripts/MIDIio.ini) contains NCalc properties for configuring **MIDIio**.  
  It goes in `SimHub/NCalcScripts/`;&nbsp;  contents include:
  - `MIDIin`:        name of source MIDI device
  - `MIDIout`:       name of destination MIDI device
  - `MIDIsliders`:   MIDI CC numbers `n` whose values are to be set as `slidern` properties.  
  - `MIDIknobs`:     MIDI CC numbers `n` whose values are to be set as `knobn` properties,  
                     handled identically to `MIDIsliders`  
  - `MIDIbuttons`:   MIDI CC numbers `n` to be set as `CCn` properties and, when (values > 0), also raise events.  
  - `MIDIsendn`:     name of e.g. a ShakeIt property whose value *changes* are sent to `MIDIout`  
                      as `CCn` messages for `0 <= n < 8`.  
                     A `pingn` action will be enabled for each configured `MIDIsendn`.  
                     By mapping a `CC` **Source** to a `pingn` **Target** in SimHub's **Controls and events**,  
                     the corresponding `MIDIin` device button can be used
                     to help identify that `CCn` to a `MIDIout` application.  
  - [`MIDIecho`](#midiecho):      if `0` or not defined, all received CC values `n` not otherwised configured  
                     (in `MIDIsliders`, `MIDIknobs` or `MIDIbuttons`) are automatically created as `CCn` properties,  
                     else (`MIDIecho > 0`) unconfigured MIDI messages are forwarded from `MIDIin` to `MIDIout`.
  - [`MIDIlog`](#midilog)        Controls MIDIio's **[System Log](SimHub.txt)** [verbosity](#midilog);&nbsp; 0 is mostly only errors and 15 is maximally verbose.  
  - `MIDIsize`	     Limits routing table size between game, vJoy and MIDI
  - `MIDICCsends`    Index array of configured `MIDICCsendn`, where 0 <= n < 128
  - `MIDIvJoy`       Non-zero enables vJoy button and axes outputs		
  - `MIDIvJoybuttons` Index array of configured `MIDIvJoyB0x`, where 01 <= x < 16 Buttons
                     as reported by `MIDIio.VJsend.Init() in the **[System Log](SimHub.txt)**
  - `MIDIvJoyaxiss`   Index array of configured `MIDIvJoyaxisx`, where 0 <= x < 8 Axes
                     as reported by `MIDIio.VJsend.Init()` in the **[System Log](SimHub.txt)**  

### MIDIecho
`MIDIecho 1` forwards unconfigured `MIDIin` CC changed messages to `MIDIout`.  
Un-echoed CC messages most recently sent to `MIDIout` are saved,  
then resent when SimHub next launches the MIDIio plugin.  
This is intended to enable resuming a MIDI configuration from time to time.  
Duplicated send CC messages are NOT sent, to minimize traffic and CPU overhead.  
In `MIDIecho 1` mode, only previously configured input and output properties may be used.  
In `MIDIecho 0` mode, SimHub properties are dynamically generated for unconfigured input CC numbers, but not forwarded.  
This allows learning MIDI controller CC numbers (by checking SimHUb's **Property** window),  
for adding to `SimHub/NCalcScripts/MIDIio.ini`.  

### MIDIlog
- 4 bit flags; valid values: 0 (exceptions and other errors), 1, 3, 7, 15 (trace many actions)
- 0: exceptions not handled by code, things not working, e.g. misconfigured
- 1: also I/O failures
- 3: also unexpected events
- 7: also initialization and configuration feedback
- 15: also information, normal event tracing

### Evidence
Here is some evidence of operational success (*26 Jan 2023*):  
- **[MidiView](https://hautetechnique.com/midi/midiview/) trace screen**:  
![](MidiView.png)  

- ... for this game replay:  
![](replay.png)  

- which was prior to vJoy implementation.  

 *updated 23 Feb 2023 for vJoy*  
 *updated 29 Jun 2023 for SimHub-provoked revisions*  
 *updated 1 Feb 2024 for MIDIlog explanations*
