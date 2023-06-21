### blekenbleu.MIDIio June 2023 changes provoked by SimHub updates.

In February, SimHub `Controllers input` plugin created **JoystickPlugin** properties before `MIDIio` plugin launch.  
By June, that was no longer true.&nbsp; [Further issues were noted](../../../#simhub-v843-breakage)  
- `null JoystickPlugin` axis property issues could be addressed by changes to MIDIio.cs,  
  but code inspection revealed
- calling `DoSend()` twice when data.GameRunning is wasteful  
  - instead, change `DoSend()` index implementation  
- also in `DoSend()`:  
  - change `t` to `s` (for source)
  - should never need to ignore null game properties
    - they should never be indexed unless data.GameRunning
  - disable ALL null axis and game source properties when data.GameRunning (0 == index)
  - threshold property values at 50% for Joystick buttons
- rename vJoy output configuration properties to include "vJoy" instead of "vJD" and e.g. "vJoyB01" instead of "vJDbutton1"
- Input Joystick B05 affects vJoy B02 instead of configured B03

After changes for all but that last,
- MIDI input is dead..? fixed by replugging nanoKONTROL2
- log messages have only vJoy button 3 configured;  missing 1 and 5
- vJoy y axis output is broken; responding to MIDI button 0, which should be:
  vJoy B01
- vJoy B02 does respond to SideWinder slider, as does vJoy axis 2 (Z axis)
- vJoy B03 does not respond to SideWinder B05 (base rear center)
- vJoy B05 does not respond to CC43 (lowest leftmost button)
