[*back*](principles.md#midiio-events-and-actions)
## MIDIio Actions and Events
To use Actions and Events configured in `MIDIio.ini`, configure them in SimHub **Controls and events**  
### `MIDIio.ini` Actions and Events examples:
```
string comment = "used to be name='MIDICCsends';  vJoy axis prefixed by A, vJoy button by B, CC by C"
[ExportProperty]
name='MIDIsends'
value='A2,A3,B2,B22,C21,C4'

string value = "set some SimHub property for Action values to send and changes to trigger Events"
[ExportProperty]
name='sendA2'
value='MIDIio.slider2'

string vJoySendSider = "Action and Event for otherwise unconfigured MIDI slider:  CC 7"
[ExportProperty]
name='sendA3'
value='MIDIio.CC7'

string B2 = "defines source property for sendB2 Action and sendB2 Event"
[ExportProperty]
name='sendB2'
value='JoystickPlugin._VKBsim_Gladiator_EVO_R___B25'

[ExportProperty]
name='sendB22'
value='MIDIio.button6'

[ExportProperty]
name='sendC21'
value='JoystickPlugin._VKBsim_Gladiator_EVO_R___B26'

string changed = "used to be name='MIDICCsend2'"
[ExportProperty]
name='sendC2'
value='ShakeITBSV3Plugin.Export.OutputSlip.FrontLeft'
```
MIDIio triggers `sendC*` Events as `MIDIin` CC changes are received;  
during `DataUpdate()`s, MIDIio triggers other Events for configured SimHub property changes.

