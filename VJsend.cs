/*
 ;
 ; This project is ripped from vJoy's original simple C# feeder app
 ; Since MIDIio will generally handle only a single button or axis at a time, EFFICIENT would not be.
 ; ROBUST employs functions that are easy and safe to use
 ;
 ; Functionality:
 ;	The program starts with creating one joystick object.
 ;	Then it fetches the device id from the command-line and makes sure that it is within range
 ;	After testing that the driver is enabled it gets information about the driver
 ;	Gets information about the specified virtual device
 ;	This feeder uses only a few axes. It checks their existence and
 ;	checks the number of buttons and POV Hat switches.
 ;	Then the feeder acquires the virtual device
 ;	Here starts and endless loop that feedes data into the virtual device
 ;
 ;-----------------------------------------------------------------------------------------------------*/
//#define FFB

using System;
using vJoyInterfaceWrap;		// Don't forget to add this

//namespace FeederDemoCS
namespace blekenbleu.MIDIspace
{
    class VJsend
    {
        internal vJoy joystick;		// Declare one joystick (Device id 1) and a position structure.
        internal vJoy.JoystickState iState;
        private uint id;
        private readonly HID_USAGES[] usages = {HID_USAGES.HID_USAGE_X, HID_USAGES.HID_USAGE_Y, HID_USAGES.HID_USAGE_Z, HID_USAGES.HID_USAGE_RX, HID_USAGES.HID_USAGE_RY,
                                   HID_USAGES.HID_USAGE_RZ, HID_USAGES.HID_USAGE_SL0, HID_USAGES.HID_USAGE_SL1, HID_USAGES.HID_USAGE_WHL, HID_USAGES.HID_USAGE_POV };
        private readonly string[] HIDaxis = { "X", "Y", "Z", "RX", "RY", "RZ", "SL0", "SL1", "WHL", "POV" };
        private MIDIio M;
        private long maxval;
        private uint count, nButtons, nAxes;
        private HID_USAGES[] Usage;
        private int[] AxVal;

        internal void Init(MIDIio that, uint ID)
        {
            M = that;
            id = ID;	// Device ID can only be in the range 1-16
            AxVal = new int[usages.Length];
            nAxes = 0;
            maxval = 0;
            Usage = new HID_USAGES[usages.Length];

            // Create one joystick object and a position structure.
            joystick = new vJoy();
            iState = new vJoy.JoystickState();
#if FFB
            FFBReceiver = new VJoyFFBReceiver();
            if (joystick.IsDeviceFfb(id)) {	// Start FFB

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
            if (id <= 0 || id > 16) {
                SimHub.Logging.Current.Info($"Invalid device ID;  must be 0 < {id} <= 16");
                return;
            }

            // Get the driver attributes (Vendor ID, Product ID, Version Number)
            if (!joystick.vJoyEnabled()) {
                SimHub.Logging.Current.Info($"vJoy driver not enabled: Failed Getting vJoy attributes.");
                return;
            } else
                M.Log(4, $"Vendor: {joystick.GetvJoyManufacturerString()}\nProduct :{joystick.GetvJoyProductString()}\nVersion Number:{joystick.GetvJoySerialNumberString()}");

            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(id);
            switch (status) {
                case VjdStat.VJD_STAT_OWN:
                    SimHub.Logging.Current.Info($"vJoy Device {id} is already owned by this feeder");
                    break;
                case VjdStat.VJD_STAT_FREE:
                    M.Log(4, $"vJoy Device {id} is available, with capabilities:");
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    SimHub.Logging.Current.Info($"vJoy Device {id} is already owned by another feeder\nCannot continue");
                    return;
                case VjdStat.VJD_STAT_MISS:
                    SimHub.Logging.Current.Info($"vJoy Device {id} is not installed or disabled\nCannot continue");
                    return;
                default:
                    SimHub.Logging.Current.Info($"vJoy Device {id} general error\nCannot continue");
                    return;
            };
 
            // Get button count and count axes for this vJoy device
            nButtons = (uint)joystick.GetVJDButtonNumber(id);
            // GetVJDAxisExist(), () respond only to HID_USAGES Enums, not the equivalent integers..?
            string got = "";
            for (uint i = nAxes = 0; i < usages.Length; i++)
            {
                AxVal[i] = 0;
                if (joystick.GetVJDAxisExist(id, usages[i]))	// which axes are supported?
                {
                    Usage[nAxes++] = usages[i];
                    if (1 < nAxes)
                        got += ", ";
                    else got += " available: ";
                    got += HIDaxis[i];
                }
            }
            M.Log(4, $"  {nButtons} Buttons, {nAxes} Axes{got}");

            // Test if DLL matches the driver
            UInt32 DllVer = 0, DrvVer = 0;
            bool match = joystick.DriverMatch(ref DllVer, ref DrvVer);
            if (match)
                M.Log(4, $"Driver version {DrvVer:X} Matches DLL Version {DllVer:X}");
            else SimHub.Logging.Current.Info($"Driver version {DrvVer:X} does NOT match DLL version ({DllVer:X})");

            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
            {
                SimHub.Logging.Current.Info($"Failed to acquire vJoy device number {id}.");
                return;
            }
            else M.Log(4, $"Acquired: vJoy device number {id}.");
#if FFB
            StartAndRegisterFFB();
#endif
            count = 0;
            joystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_X, ref maxval);

            // Reset this device to default values
            joystick.ResetVJD(id);
        }		// Init()

        internal void Run()
        {
            int[] inc = { 150, 250, 350, 220, 200, 180, 165, 300, 330 };
            if (0 == maxval)
                return;

            count++;
            if (0 < (31 & count))
                return;
            M.Log(8, $"VJd.Run(): count = {count}");

            // Feed the device in endless loop
            // Set axes positions
            for (int i = 0; i < nAxes; i++)
            {
                AxVal[i] += inc[i];
                if (maxval < AxVal[i])
                    AxVal[i] = 0;
                joystick.SetAxis(AxVal[i], id, Usage[i]);    // HID_USAGES Enums
            }

            // Press/Release Buttons
            uint set = 1 + (count >> 5) % nButtons, unset = 1 + (1 + (count >> 5)) % nButtons;
            M.Log(8, $"VJd.Run(): set {set};  unset {unset}");
            joystick.SetBtn(true, id, set);			// 1 <= nButtons <= 32
            joystick.SetBtn(false, id, unset);
        } // Run()

        internal void End()
        {
            joystick.RelinquishVJD(id);
        }
    }		// class VJsend
}		// namespace FeederDemoCS
