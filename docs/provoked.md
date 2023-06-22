### blekenbleu.MIDIio June 2023 changes provoked by SimHub updates.

In February, SimHub `Controllers input` plugin created **JoystickPlugin** properties before `MIDIio` plugin launch.  
By June, [that was no longer true](../../../#simhub-v843-breakage).  
- `null JoystickPlugin` axis property issues could be addressed by changes to MIDIio.cs,  
  but code inspection revealed
- add blank lines between Init() log messages
- calling `DoSend()` twice when data.GameRunning is wasteful  
  - instead, change `DoSend()` index implementation  
- also in `DoSend()`:  
  - change `t` to `s` (for source)
  - should never need to ignore null game properties
    - they are indexed only when data.GameRunning
  - disable ALL null axis and game source properties when data.GameRunning (0 == index)
  - button input properties will be null until changed
  - threshold property values at 50% to send as Joystick buttons
- rename vJoy output configuration properties to include "vJoy" instead of "vJD" and e.g. "vJoyB01" instead of "vJDbutton1"
- Input Joystick B05 affects *SimHub* vJoy B02 instead of configured B03

*21 Jun 2023*:&nbsp; After changes for all but that last,
- MIDI input is dead..? fixed by replugging nanoKONTROL2
- log messages have only vJoy button 3 configured;  missing 1 and 5
- vJoy y axis output is broken; responding to MIDI button 0, which should be:
  vJoy B01
- vJoy B02 does respond to SideWinder slider, as does vJoy axis 2 (Z axis)
- vJoy B03 does not respond to SideWinder B05 (base rear center)
- vJoy B05 does not respond to CC43 (lowest leftmost button)  

*22 Jun 2023*  
- fixed MIDIsize setting error checking
- fixed button inputs in MIDIio.IOproperties.Init() - wrong name (MIDIvJoybutton vs MIDIvJoyB0)  
  this corrected most 21 Jun errors...
- added game property inputs to vJoy button and axis
- SimHub reports vJoy buttons 0-based, e.g.:  
  - Button 1 in `joy.cpl` vJoy Device properties is SimHub **InputStatus** `JoystickPlugin.vJoy_Device_B00`  
  - changed NCalcScript vJoy button numbering to SimHub convention, now mismatches vJoy library...
