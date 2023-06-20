### blekenbleu.MIDIio [SimHub](https://www.simhubdash.com/) plugin:&nbsp; now with Joystick support
 For one MIDI input and one MIDI destination device, this [SimHub](https://github.com/SHWotever/SimHub) plugin routes configured Button, Slider and Knob  
 [Control Change](https://www.midi.org/specifications-old/item/table-3-control-change-messages-data-bytes-2) (CC) messages,
 to e.g. on-the-fly tweak suitably customized **ShakeIt Bass Shaker** effects.  
Similarly, SimHub properties can be configured for buttons and axes from one Joystick input device.  
Unconfigured CC messages received dynamically generate new properties or are forwarded to a target Device.  
MIDI C# code evolved from SimHub's `User.PluginSdkDemo`,
using [`Melanchall.DryWetMidi`](https://github.com/melanchall/drywetmidi)'s DLL (already in SimHub).  

MIDIio also generates [DirectInput](https://blekenbleu.github.io/Windows/HID.md) Button and Axis joystick inputs for games,
reusing [C# sample code](https://github.com/blekenbleu/vJoySDK) from [vJoy](https://github.com/njz3/vJoy).  
MIDI CCs and [vJoy](https://blekenbleu.github.io/Windows/HID) sends are rescaled values from configured SimHub properties,
 e.g. [**ShakeIt Bass Shaker** effects](https://github.com/SHWotever/SimHub/wiki/ShakeIt-V3-Effects-configuration).

[Motivation and development How-To's](https://blekenbleu.github.io/MIDI/plugin/)  
[**MIDIio** Source code files, configuration descriptions](docs/source.md)  
[principles of operation](docs/principles.md)  
[June 2023 revisions provoked by SimHub updates](docs/provoked.md)

#### Notes:
- This plugin **is compatible with SimHub 8.2.2's `Controllers input` and `Control mapper` plugins**  
  - This allows e.g. forwarding *real* `Controllers input` properties to MIDIout or vJoy;  
    **Do NOT** configure *vJoy* properties from `Controllers input`;&nbsp; that would provoke feedback loops!  
- This plugin is **incompatible with SimHub's Midi Controllers Input plugin**  
    - Enabling both causes SimHub to crash!!!   
- Like SimHub's `Midi Controllers Input` plugin,  
  **MIDIio** can automatically set CCn properties  for received CCn messages not already configured,  
  but (unlike SimHub's) from only the single configured MIDI device.  
  This is expected to be used mostly for identifying CC numbers to configure.
- SimHub bundles vJoy DLL v2.1.8, while [vJoy is v2.1.9.1 is available](https://sourceforge.net/projects/vjoystick/).  
- This plugin is configured using SimHub properties;&nbsp; it has NO interactive interface window.
    - configure this plugin by editing [`NCalcScripts\MIDIio.ini`](blob/main/NCalcScripts/MIDIio.ini), which goes in `SimHub\NCalcScripts\` folder 
    - **check System log for MIDI-related messages:**  
      ![log messages](docs/log.png)  

    - **check Available properties for MIDI-related values**:
      ![Properties values](docs/properties.png)

    - **Configure button `CCn` Source events:**  
      ![button event names and actions](docs/events.png)  
    - this is *not* (nor can it become) a "plug and play" solution;  
      configuring MIDI on Windows is [**very much DIY**](https://www.racedepartment.com/threads/simhub-plugin-s-for-output-to-midi-and-vjoy.210079/).

For testing, [this ShakeIt profile has a custom effect](https://github.com/blekenbleu/SimHub-profiles/blob/main/Any%20Game%20-%20MIDIio_proxyLS.siprofile) with ShakeITBSV3Plugin properties from MIDI sliders.

*18 Jun 2023*  
#### SimHub v8.4.3 breakage  
- JoystickPlugin properties are unavailable before MIDIio plugin Init() exits:
```
[2023-06-18 09:52:41,546] INFO - MIDIio.DoSend(vJDaxis): null JoystickPlugin.SideWinder_Precision_2_Joystick_X for SourceName[1][0]^M
[2023-06-18 09:52:41,546] INFO - MIDIio.DoSend(vJDaxis): null JoystickPlugin.SideWinder_Precision_2_Joystick_Y for SourceName[1][1]^M
[2023-06-18 09:52:41,546] INFO - MIDIio.DoSend(vJDaxis): null JoystickPlugin.SideWinder_Precision_2_Joystick_Slider0 for SourceName[1][2]^M
[2023-06-18 09:52:41,551] INFO - Game successfully loaded^M
[2023-06-18 09:52:42,071] INFO - JoystickManager : Found Load_Cell_Interface_LC-USB, SideWinder_Precision_2_Joystick, vJoy_Device, T500_RS_Gear_Shift^M 
```
- MIDIio.ini 'MIDIvJDbutton3' generates property named `InputStatus.JoystickPlugin.vJoy_Device_B02`
- add blanks between `INFO`s
- [`Poller.cs` thread class used by `ThrustmasterLEDControllerPlugin.cs`](https://gitlab.com/prodigal.knight/simhub-thrustmaster-wheel-led-controller) plugin
