[*back*](../README.md)
### MIDI device finder, CC mapper

MIDIio has UI neither for finding nor mapping MIDI
- MIDIio uses [melanchall / drywetmidi](https://melanchall.github.io/drywetmidi/articles/devices/Overview.html),
	for which SimHub's version lacks 
	[`GetAll()`](https://melanchall.github.io/drywetmidi/api/Melanchall.DryWetMidi.Multimedia.InputDevice.html#Melanchall_DryWetMidi_Multimedia_InputDevice_GetAll)
- while less nicely documented than drywetmidi, [NAudio MidiInAndOut](https://github.com/naudio/NAudio/blob/master/Docs/MidiInAndOut.md) supports multiple MIDI device input
	- get all input MIDI [devices](https://github.com/blekenbleu/OpenKneeboard-SimHub-plugin-menu/blob/MIDI/MIDI.cs)
### [**queue** multiple MIDI device inputs by `System.Threading.Channels`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.channels):
  [nuget](https://www.nuget.org/packages/System.Threading.Channels)
	- [stackexchange example](https://codereview.stackexchange.com/a/295445)  
	- ["There are very few reasons to prefer the older BufferBlock<T> over the newer Channel<T>"](https://stackoverflow.com/a/76394149)
	- [Performance comparison favoring `Channels`](https://michaelscodingspot.com/performance-of-producer-consumer/)
	- [Medium:  C# Channels Explained](https://medium.com/@abhirajgawai/c-channels-explained-from-producer-consumer-basics-to-high-performance-net-systems-f8ab610c0639)
	- [Why Channels Over BlockingCollections](https://dev.to/chakewitz/advanced-c-concurrency-channels-pipelines-and-parallel-processing-218n)
		- `async/await`, high performance, customizeable

#### discounted multiple MIDI input device alternatives
- [midi-feeder](https://github.com/blekenbleu/midi-feeder) does not detect Bluetooth MIDI devices
- [MidiMapper - .NET 6](https://github.com/JeanChristopheVOISIN/MidiMapper)
- [MIDIFlux - supports Windows MIDI Services](https://github.com/Cozmopolit/MIDIFlux)
- [MIDI sample -m$](https://github.com/microsoft/windows-universal-samples/tree/main/Samples/MIDI)
- drywetmidi methods handle events from only [single devices](https://melanchall.github.io/drywetmidi/api/Melanchall.DryWetMidi.Multimedia.InputDevice.html)
	- consequently multiple [input device](https://melanchall.github.io/drywetmidi/articles/devices/Input-device.html) tasks ... feeding a queue?
	- [codeproject - DryWetMIDI: Working with MIDI Devices](https://main.codeproject.com/articles/DryWetMIDI-Working-with-MIDI-Devices)

#### sharing input devices
Currently all M$ Human Interface Devices (HID), including MIDI, can connect to only one application at a time.
- [Windows MIDI Services](https://microsoft.github.io/MIDI/kb/) will add [simultaneous multi-client access to devices](https://midi.org/midi-2-0-coming-to-windows-11)
- many real MIDI devices support passthru
- MIDI devices can have up to 16 channels
- could pass any input device events to a drywetmidi output device,  
	with a separate output channel for each input device.

#### multi-state WPG
- use the same .xaml, but change code-behind for assigning buttons and sliders to MIDI or joystick events
	- [Solution 1: Strategy + Singleton or Solution 2: delegates](https://learn.microsoft.com/en-us/archive/blogs/kirillosenkov/how-to-override-static-methods)
	- [XAML Events](http://www.diranieh.com/NET_WPF/Events.htm)
	- [stackoverflow:  WPF custom routed event](https://stackoverflow.com/a/44616505)
	- [WPF event routing strategy:  Bubble, Tunnel, or Direct](https://www.tutorialspoint.com/wpf/wpf_routed_events.htm)
