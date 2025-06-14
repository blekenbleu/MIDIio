﻿/*
 ; This class is ripped from vJoy's original simple C# feeder app
 ; MIDIio will generally handle only a single button or axis at a time;  EFFICIENT would not be.
 ;
 ; Functionality:
 ;		Init() checks for valid device 0 < id <= 16, then creates a joystick object.
 ;		After testing that the driver is enabled,
 ;		Init() gets and reports information about it and specified virtual device
 ;		Init() then checks axes existence and button count, then acquires and resets the virtual device.
 ;
 ;		Loop() exercises available buttons and axes of that virtual device.
 ;
 ;-----------------------------------------------------------------------------------------------------*/
//#define FFB

using System;
using vJoyInterfaceWrap;				// Don't forget to add this

//namespace FeederDemoCS
namespace blekenbleu
{
	class VJsend
	{
		internal vJoy joystick;					// Declare one joystick (Device id 1) and a position structure.
		private uint id;
		private readonly HID_USAGES[] usages = {HID_USAGES.HID_USAGE_X, HID_USAGES.HID_USAGE_Y,
								   HID_USAGES.HID_USAGE_Z, HID_USAGES.HID_USAGE_RX, HID_USAGES.HID_USAGE_RY,
								   HID_USAGES.HID_USAGE_RZ, HID_USAGES.HID_USAGE_SL0, HID_USAGES.HID_USAGE_SL1,
								   HID_USAGES.HID_USAGE_WHL, HID_USAGES.HID_USAGE_POV };
		private readonly string[] HIDaxis = { "X", "Y", "Z", "RX", "RY", "RZ", "SL0", "SL1", "WHL", "POV" };
		private long maxval;
		private uint count;
		internal byte nButtons, nAxes;
		internal HID_USAGES[] Usage;
		private int[] AxVal;

		internal long Init(uint ID)				// return maxval
		{
			nAxes = 0;
			maxval = 0;
			bool acquire = false;

			if (ID <= 0 || ID > 16)
				return MIDIio.Info($"VJsend(): Invalid device ID;  must be 0 < {ID} <= 16") ? 0 : 0;
			id = ID;							// Device ID can only be in the range 1-16

			// Create one joystick object and a position structure.
			joystick = new vJoy();

			// Get driver attributes (Vendor ID, Product ID, Version Number)
			if (!joystick.vJoyEnabled())
				return MIDIio.Info($"VJsend(): vJoy driver not enabled: Failed Getting vJoy attributes.") ? 0 : 0;

			string s = "VJsend.Init(): Found " + joystick.GetvJoyProductString();
			s += $"\nvJoy.Version: {joystick.GetvJoySerialNumberString()}\n"
				   + $"Developer: {joystick.GetvJoyManufacturerString()}\n";
			

			// Test if DLL matches the driver
			UInt32 DllVer = 0, DrvVer = 0;
			if (joystick.DriverMatch(ref DllVer, ref DrvVer))
				s += $"vJoy driver version {DrvVer:X} Matches DLL Version {DllVer:X}\n";
			else s += $"Driver version {DrvVer:X} does NOT match DLL version ({DllVer:X})\n";

			// Get the state of the requested device
			VjdStat status = joystick.GetVJDStatus(id);
			switch (status) {
				case VjdStat.VJD_STAT_OWN:
					s += $"vJoy Device {id} is already owned by this feeder, with capabilities:\n";
					break;
				case VjdStat.VJD_STAT_FREE:
					s += $"vJoy Device {id} is available with capabilities:\n";
					acquire = true;
					break;
				case VjdStat.VJD_STAT_BUSY:
					return MIDIio.Info(s += $"vJoy Device {id} is already owned by another feeder;  cannot continue\n") ? 0:0;
				case VjdStat.VJD_STAT_MISS:
					return MIDIio.Info(s += $"vJoy Device {id} is not installed or disabled;  cannot continue\n") ? 0:0;
				default:
					return MIDIio.Info(s += $"vJoy Device {id} general error;  cannot continue\n") ? 0:0;
			}
 
#if FFB
			FFBReceiver = new VJoyFFBReceiver();
			if (joystick.IsDeviceFfb(id)) {		// Start FFB

				// Register Generic callback function
				// At this point you instruct the Receptor which callback function to call with every FFB packet it receives
				// It is the role of the designer to register the right FFB callback function

				// Note from me:
				// Warning: the callback is called in the context of a thread started by vJoyInterface.dll
				// when opening the joystick. This thread blocks upon a new system event from the driver.
				// It is perfectly ok to do some work in it, but do not overload it to avoid
				// loosing/desynchronizing FFB packets from the third party application.
				FFBReceiver.RegisterBaseCallback(joystick, id);
			}
#endif // FFB
			Usage = new HID_USAGES[usages.Length];
			AxVal = new int[usages.Length];

			// Get button count, and count axes for this vJoy device
			nButtons = (byte)joystick.GetVJDButtonNumber(id);

			// GetVJDAxisExist() responds only to HID_USAGES Enums, not equivalent integers..?
			string got = "";
			for (uint i = nAxes = 0; i < usages.Length; i++)
			{
				AxVal[i] = 0;
				if (joystick.GetVJDAxisExist(id, usages[i]))		// which axes are supported?
				{
					Usage[nAxes++] = usages[i];
					if (1 < nAxes)
						got += ", ";
					else got += " available: ";
					got += HIDaxis[i];
				}
			}

			joystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_X, ref maxval);
			s += $"  {nButtons} Buttons; {nAxes} Axes{got}; axis maxval={maxval}.\n";
			if (acquire)		// Acquire the target?
			{
				if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
					return MIDIio.Info(s + $"\nVJsend.Init(): Failed to acquire vJoy device number {id}.\n") ? 0 : 0;
				else MIDIio.Log(4, s + $"vJoy Device {id} is now acquired.\n");
			}
			else MIDIio.Log(4, s + "\n");
#if FFB
			StartAndRegisterFFB();
#endif
			// Reset this device to default values
			joystick.ResetVJD(id);

			count = 0;
			return maxval;
		}						// Init()

		internal void Loop()
		{
			int[] inc = { 150, 250, 350, 220, 200, 180, 165, 300, 330 };
			if (0 == maxval)
				return;

			count++;
			if (0 < (31 & count))
				return;
			MIDIio.Log(4, $"VJd.Loop(): count = {count}");

			// Feed the device in endless loop
			// Set axes positions
			for (int i = 0; i < nAxes; i++)
			{
				AxVal[i] += inc[i];
				if (maxval < AxVal[i])
					AxVal[i] = 0;
				joystick.SetAxis(AxVal[i], id, Usage[i]);	// HID_USAGES Enums
			}

			// Press/Release Buttons
			uint set = 1 + (count >> 5) % nButtons, unset = 1 + (1 + (count >> 5)) % nButtons;
			MIDIio.Log(4, $"VJd.Loop(): set {set};  unset {unset}");
			joystick.SetBtn(true, id, set);						// 1 <= nButtons <= 32
			joystick.SetBtn(false, id, unset);
		} 														// Loop()

		internal void Axis(byte axis, int valint)
		{
			joystick.SetAxis(valint, id, Usage[axis]);				// 0 <= valing <= maxval
		}

		internal void Button(byte button, bool value)
		{
			joystick.SetBtn(value, id, button);						// 1 <= button <= 32
		}

		internal void End()
		{
			joystick.RelinquishVJD(id);
		}
	}				// class VJsend
}				// namespace FeederDemoCS
