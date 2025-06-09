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
- In `ReceivedCC(CC, value)	//` Calls `Send()` or `TriggerEvent()`:  
`0 < (14 & Which[CC])		//` configurable routing bits (`8 + 4 + 2 = 14`)  
`Map[CC]			//` translates source CC to `ListCC` address   
`ListCC[route][Map[CC]]		//` destination address;&nbsp; `0 <= route <= 2`  
`CCevent[CC]			//` maps SendEvent input CC to Event number for Event Trigger  

- In `ReceivedCC(CC, value)` and `Act(a)`:  
`CCname[CC]			//` source name  
`CCvalue[CC]			//` most recently received value  

- In `Act(a)`, for Actions based on CC inputs:   
`CC = ActList[a][1]		//` maps Action `a` to source CC number  
`dev = ActList[a][2]		//` destination device  
`addr = ActList[a][3]		//` destination device button or axis number

