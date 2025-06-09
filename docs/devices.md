[*back*](principles.md)  
### MIDIio devices
MIDIio exclusively binds to no more than one MIDI source, MIDI destination and vJoy destination device.  
Joystick and button box DirectInput values are configured from SimHub **Controllers** plugin property names.  
From that MIDI source, MIDIio handles *only* MIDI CCs;  
- Split MIDI keyboard output
  e.g. by [loopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html),
  to use it for more than CCs to MIDIio.
- [vJoy supports more than one virtual DirectInput device](https://github.com/SHWotever/SimHub/wiki/Control-Mapper-plugin#vjoy),  
	but MIDIio lacks support for specifying *which* vJoy device it uses.  
- MIDIio asynchronously routes CC changes from `MIDIin` as detected.  
- MIDIio routes configured `JoystickPlugin` input changes during `MIDIio.DataUpdate()` (typically @ 60Hz).  
- MIDIio routes non-Joystick property value changes only while a game (or replay) runs.  
- [MIDIio Events and Actions](sends.md) use these same devices.
