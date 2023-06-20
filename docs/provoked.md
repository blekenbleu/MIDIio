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
