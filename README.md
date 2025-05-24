### MIDIio [SimHub](https://www.simhubdash.com/) plugin:&nbsp; now with Joystick support

**Note**: &nbsp;  *requires a recent (9.X) version* of [SimHub](https://www.simhubdash.com/download-2/)  

For one MIDI source device and one MIDI destination device,  
 this [SimHub](https://github.com/SHWotever/SimHub) plugin routes configured Button, Slider and Knob  
 [Control Change](https://www.midi.org/specifications-old/item/table-3-control-change-messages-data-bytes-2) (CC) messages,
 for e.g. on-the-fly tweaking suitably customized **ShakeIt Bass Shaker** effects.  
Similarly, SimHub properties can be configured for buttons and axes from one Joystick device.  
Unconfigured CC messages received either
- dynamically generate new properties
- or are forwarded to a target Device.  

MIDI C# code evolved from SimHub's `User.PluginSdkDemo`,
using [`Melanchall.DryWetMidi`](https://github.com/melanchall/drywetmidi)'s DLL (already in SimHub).  

MIDIio *also* generates [DirectInput](https://blekenbleu.github.io/Windows/HID.md) Button and Axis joystick inputs for games,
reusing [C# sample code](https://github.com/blekenbleu/vJoySDK) from [vJoy](https://github.com/njz3/vJoy).  
MIDI CCs and [vJoy](https://blekenbleu.github.io/Windows/HID) sends are rescaled values from configured SimHub properties,
 e.g. [**ShakeIt Bass Shaker** effects](https://github.com/SHWotever/SimHub/wiki/ShakeIt-V3-Effects-configuration).

[Motivation and development How-To's](https://blekenbleu.github.io/MIDI/plugin/)  
[**MIDIio** Source code files, configuration descriptions](docs/source.md)  
[principles of operation](docs/principles.md)  
[June 2023 revisions provoked by SimHub updates](docs/provoked.md)

#### Notes:
- This plugin **was compatible with SimHub 8.4.3's `Controllers input` and `Control mapper` plugins**  
  - It has not been comprehensively tested for SimHub changes since then
  - This allows e.g. forwarding *real* `Controllers input` properties to MIDIout or vJoy;  
    **Do NOT** configure *vJoy* properties from `Controllers input`;&nbsp; that would provoke feedback loops!  
- This plugin is **incompatible with SimHub's `Midi Controllers Input` plugin**  
    - Enabling both causes SimHub to crash!!!   
- Like SimHub's **`Midi Controllers Input`** plugin,  
  **MIDIio** can automatically set CCn properties  for received CCn messages not already configured,  
  but (unlike SimHub's) from **only the single configured MIDI device**.  
  Unconfigured CCs are expected to be used mostly for identifying CC numbers to configure.
- SimHub bundles vJoy DLL v2.1.8, while [vJoy is v2.1.9.1 is available](https://sourceforge.net/projects/vjoystick/).  
- This plugin is configured using SimHub properties;&nbsp; it has NO interactive interface window.
    - configure this plugin by editing [`NCalcScripts\MIDIio.ini`](blob/main/NCalcScripts/MIDIio.ini), which goes in `SimHub\NCalcScripts\` folder 
    - **check [System log](docs/SimHub.txt) for MIDI and/or vJoy related messages:**  
      ![log messages](docs/log.png)  

    - **check Available properties for MIDI-related values**:
      ![Properties values](docs/properties.png)

    - **Configure button `CCn` Source events:**  
      ![button event names and actions](docs/events.png)  
    - this is *not* (nor can it become) a "plug and play" solution;  
      configuring MIDI on Windows is [**very much DIY**](https://www.racedepartment.com/threads/simhub-plugin-s-for-output-to-midi-and-vjoy.210079/).  
- **vJoy button numbering**  
    - Windows' `joy.cpl` and vJoy API consider the first button to be 1,  
      but SimHub reports that first button as `JoystickPlugin.vJoy_Device_B00`:  
      ![vJoy](docs/vJoyB.png)  
    - For consistency within SimHub, it is configured as `MIDIvJoyB00` in [NCalcScripts/MIDIio.ini](NCalcScripts/MIDIio.ini).  

For testing, [this ShakeIt profile](https://github.com/blekenbleu/SimHub-profiles/blob/main/Any%20Game%20-%20MIDIio_proxyLS.siprofile)
 has a custom effect with ShakeITBSV3Plugin properties from MIDI sliders.

*18 Jun 2023*  
#### SimHub v8.4.3 breakage  
- JoystickPlugin properties are unavailable before MIDIio plugin Init() completes:
```
[2023-06-18 09:52:41,546] INFO - MIDIio.DoSend(vJoyaxis): null JoystickPlugin.SideWinder_Precision_2_Joystick_X for SourceName[1][0]^M
[2023-06-18 09:52:41,546] INFO - MIDIio.DoSend(vJoyaxis): null JoystickPlugin.SideWinder_Precision_2_Joystick_Y for SourceName[1][1]^M
[2023-06-18 09:52:41,546] INFO - MIDIio.DoSend(vJoyaxis): null JoystickPlugin.SideWinder_Precision_2_Joystick_Slider0 for SourceName[1][2]^M
[2023-06-18 09:52:41,551] INFO - Game successfully loaded^M
[2023-06-18 09:52:42,071] INFO - JoystickManager : Found Load_Cell_Interface_LC-USB, SideWinder_Precision_2_Joystick, vJoy_Device, T500_RS_Gear_Shift^M 
```
- changes described [here](docs/provoked.md)  
- [`Poller.cs` thread class used by `ThrustmasterLEDControllerPlugin.cs`](https://gitlab.com/prodigal.knight/simhub-thrustmaster-wheel-led-controller) plugin  

*1 Feb 2024*  
- [reduced log verbosity](docs/source.md#midiioini)

*24 May 2025* `version 0.0.1.3`
- tested vJoy axis with JSONio property;&nbsp; 0 to 100 range expected  
- changed example MIDIio.ini to use VKB instead of SideWinder HOTAS  
- test for non-zero `MIDIvJoy` setting (non-NULL VJD`) before logging missing `MIDIvJoybuttons` or `MIDIvJoyaxiss`  
- bug fix:  test for null Darray[0-1] instead of (never) null Darray
