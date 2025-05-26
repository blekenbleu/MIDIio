[*back*](principles.md#run-time-operation)  
### IOproperties.cs `Which[]` byte array
Minimize processing in `MIDIio.cs Active()` for each received CC by precalculating.  
The `Which[]` byte array stores 3 type and 3 routing bit flags for each CC number.
```
1 = CC  
2 = button  
4 = unconfigured  
8 = vJoy axis		// route 0  
16 = vJoy button	// route 1  
32 = CC			// route 2  
```
#### processing
`0 < (56 & Which[d])	//` means configured routing (`8 + 16 + 32 = 56`)    
`Map[CC]		//` translates source CC to destination CC  
`CCarray[route, Map[CC]]//` destination index  

