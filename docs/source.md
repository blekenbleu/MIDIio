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
  It goes in `SimHub/NCalcScripts/`;&nbsp; contents include:
  - `MIDIin`:&nbsp; MIDI source device name
  - `MIDIout`:&nbsp; MIDI destination device name
  - `MIDIknobs`:&nbsp; MIDI CC numbers `n` whose values get set as `knobn` properties,  
  - `MIDIsliders`:&nbsp; MIDI CC numbers `n` whose values get set as `slidern` properties.  
                     handled identically to `MIDIknobs`  
  - [`MIDIbuttons`](#midibuttons):&nbsp; MIDI CC numbers `n` to be set as `CCn` properties and also raise events.  
  - [`MIDICCsendn`](#midiccsendn) `(0 <=n < 8)`:&nbsp; SimHub property name whose value *changes* are sent to `MIDIout`  
  - `MIDICCsends`:&nbsp; Index array of configured `MIDICCsendn`, where 0 <= n < 128
  - [`MIDIecho`](#midiecho):&nbsp; `MIDIin` CC message handling
  - [`MIDIlog`](#midilog):&nbsp; Controls MIDIio's **[System Log](SimHub.txt)** [verbosity](#midilog);&nbsp; 0 is mostly only errors and 7 is maximally verbose.  
  - `MIDIsize`:&nbsp; Limits routing table size between game, vJoy and MIDI
  - `MIDIvJoy`:&nbsp; Non-zero enables vJoy button and axes outputs  
  - `MIDIvJoyaxiss`:&nbsp; Index array of configured `MIDIvJoyaxisx`, where 0 <= x < 8 Axes  
                     as reported by `MIDIio.VJsend.Init()` in the **[System Log](SimHub.txt)**
  - `MIDIvJoybuttons`:&nbsp; Index array of configured `MIDIvJoyB0x`, where 01 <= x < 16 Buttons  
                     as reported by `MIDIio.VJsend.Init()` in the **[System Log](SimHub.txt)**  

### MIDIbuttons
- `MIDIbuttons` **Source** events can be mapped to **Target** actions in SimHub's **Controls and events**.  
**SimHub limitation:**&nbsp; event and action handling works only during games or replays

### MIDICCsendn
- A `MIDIio.sendn` **Target** action is enabled for each configured `MIDICCsendn`.  
**SimHub limitation:**&nbsp; event and action handling works only during games or replays

### MIDIecho
- most recent messages sent to  `MIDIout` for configured CCs  
  get resent when SimHub next launches the MIDIio plugin.  
  This is intended to restore MIDI device state (e.g. fader levels).
  
`MIDIecho 0`
- dynamically generate SimHub properties for all unconfigured input CC numbers received

`MIDIecho 1`
- forward unconfigured `MIDIin` CC changed messages to `MIDIout`.  

### MIDIlog
3 bit flags;&nbsp; valid values:  
- 0:&nbsp; exceptions not handled by code, things not working, e.g. misconfigured
- 1:&nbsp; also I/O failures
- 3:&nbsp; also unexpected events
- 7:&nbsp; also information, initialization and configuration feedback

### Evidence
Here is some evidence of operational success (*26 May 2025*):  
- `MIDIio.Unconfigured` property:&nbsp; the most recent unconfigured CC received  
   This allows learning MIDI controller CC numbers (by checking SimHUb's **Property** window)
- `MIDIio.SendCC` property:&nbsp; most recently sent
- `MIDIio.OnEventSent` property:&nbsp; most recent `MIDIio.OnEventSent()`
- **[MidiView](https://hautetechnique.com/midi/midiview/) trace screen** (*26 Jan 2023*):  
![](MidiView.png)  
- SimHub vjoy properties, Game Controllers control panel vJoy device properties:  
![](vJoyB.png)  

- ... for this game replay:  
![](replay.png)  

- which was prior to vJoy implementation.  

 *updated 23 Feb 2023 for vJoy*  
 *updated 29 Jun 2023 for SimHub-provoked revisions*  
 *updated 1 Feb 2024 for `MIDIlog` explanations*  
 *updated 26 May 2025 for `MIDIlog`, `MIDIbuttons` and `MIDIsend` changes*
