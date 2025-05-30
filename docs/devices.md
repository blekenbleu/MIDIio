[*back*](principles.md)  
### MIDIio devices
MIDIio exclusively binds to no more than one MIDI source, MIDI destination and vJoy destination device.
Joystick and button box DirectInput values are configured from SimHub **Controllers** plugin property names.
MIDIio handles only MIDI CCs;  
- if e.g. wanting to use CCs from a MIDI keyboard, its output must be split,
  e.g. by [loopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html)  
  for other MIDI devices to use its non-CC MIDI signals  
- vJoy supports more than one device, but this has not been tested.  
- MIDIio routes CC changes from `MIDIin` as soon as detected  
- MIDIio routes Joystick property value changes each time invoked by SimHub (e.g. 60Hz)  
- MIDIio routes non-Joystick property changes only while games (or replay) runs

