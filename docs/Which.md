[*back*](principles.md#run-time-operation)  
### IOproperties.cs `Which[]` byte array
Minimize `MIDIio.cs` run time processing for each received CC.  
The `Which[]` byte array stores 3 type and 3 routing bit flags for each CC number.
```
1 = CC  		// configured
2 = vJoy axis		// route 0  
4 = vJoy button		// route 1  
8 = CC			// route 2  
0x40 = SendEvent  
0x80 = Unc		// unconfigured CC received
```
#### processing
- In `ActionCC(CC, value)		//` Calls `Send()` or `TriggerEvent()`:  
`0 < (14 & Which[CC])		//` configurable routing bits (`8 + 4 + 2 = 14`)  
`Map[CC]			//` translates source CC to destination address   
`ListCC[route][Map[CC]]		//` destination address  
`CCevent[CC]			//` maps SendEvent CC to Event number for Event Trigger  

- In `ActionCC(CC, value)` and `Act(a)`:  
`CCname[CC]			//` source name  
`CCvalue[CC]			//` most recently received value  

- In `Act(a)`:   
`CC = ActMap[a][2]		//` maps Action `a` to CC number  

