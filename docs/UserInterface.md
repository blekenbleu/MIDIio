### MIDIio User Interface
Requiring users to sort input and output devices elsewhere is a bit much.  

[DryWetMIDI](https://github.com/melanchall/drywetmidi) has both
	[`InputDevice.GetAll()`](https://melanchall.github.io/drywetmidi/api/Melanchall.DryWetMidi.Multimedia.InputDevice.html#Melanchall_DryWetMidi_Multimedia_InputDevice_GetAll)
 and  [`OutputDevice.GetAll()`](https://melanchall.github.io/drywetmidi/api/Melanchall.DryWetMidi.Multimedia.OutputDevice.html#Melanchall_DryWetMidi_Multimedia_OutputDevice_GetAll)  
- search [DryWetMIDI: Working with MIDI Devices](https://www.codeproject.com/articles/DryWetMIDI-Working-with-MIDI-Devices)
- Official [Input device](https://melanchall.github.io/drywetmidi/articles/devices/Input-device.html), [Output device](https://melanchall.github.io/drywetmidi/articles/devices/Output-device.html)  


`//` [`author comments`](https://stackoverflow.com/questions/61574577/i-want-to-use-my-midi-piano-to-interact-with-my-windows-form-c-sharp-drywetmidi)
```
using System;
using Melanchall.DryWetMidi.Multimedia;

// ...

foreach (var inputDevice = InputDevice.GetAll())
{
	Console.WriteLine(inputDevice.Name);
	// user inteface selection process goes here
	if (selected)
	{
		inputDevice.EventReceived += OnEventReceived;
		inputDevice.StartEventsListening();
	}
}

// ...

private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
{
	var midiDevice = (MidiDevice)sender;
	Console.WriteLine($"Event received from '{midiDevice.Name}' at {DateTime.Now}: {e.Event}");
	// sort whether/how to use this event
}
```

### Visually
A single page, with collapsing sections
- each section, a navigable list, as in JSONio
