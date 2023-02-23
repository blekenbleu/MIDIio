// Extracted from VJsend.cs

//#define FFB
//#define DUMP_FFB_FRAME

using System;
using System.Runtime.InteropServices;
using vJoyInterfaceWrap;				// Don't forget to add this

//namespace FeederDemoCS
namespace blekenbleu.MIDIspace
{
	public class VJoyFFBReceiver
	{
		protected bool isRegistered = false;
		protected vJoy Joystick;
		protected uint Id;
		protected vJoy.FfbCbFunc wrapper;

#if FFB						// Hide stuff unknown to SimHub's vJoyInterfaceWrap

		vJoy.FFB_DEVICE_PID PIDBlock = new vJoy.FFB_DEVICE_PID();

		// For debugging only (dump frame content)
		private enum CommandType : int
		{
			IOCTL_HID_SET_FEATURE = 0xB0191,
			IOCTL_HID_WRITE_REPORT = 0xB000F
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct InternalFfbPacket
		{
			public int DataSize;
			public CommandType Command;
			public IntPtr PtrToData;
		}

		protected enum ERROR : uint
		{
			ERROR_SUCCESS = 0,
		}

		public void RegisterBaseCallback(vJoy joystick, uint id)
		{
			this.Joystick = joystick;
			this.Id = id;
			// Read PID block
			this.Joystick.FfbReadPID(this.Id, ref this.PIDBlock);

			if (!isRegistered) {
				this.wrapper = this.FfbFunction1; //needed to keep a reference!
				joystick.FfbRegisterGenCB(this.wrapper, IntPtr.Zero);
				this.isRegistered = true;
			}
		}

		protected void LogFormat(string text, params object[] args)
		{
			SimHub.Logging.Current.Info(String.Format(text, args));
		}

#if DUMP_FFB_FRAME
		public void DumpFrame(IntPtr data)
		{
			unsafe {
				InternalFfbPacket* FfbData = (InternalFfbPacket*)data;
				int size = FfbData->DataSize;
				int command = (int)FfbData->Command;
				byte* bytes = (byte*)FfbData->PtrToData;
				StringBuilder line = new StringBuilder();
				line.AppendFormat(String.Format("FFB Size {size}"));
				line.AppendFormat(" Cmd:" + String.Format("{(int)FfbData->Command:X08}"));
				line.AppendFormat(" ID:" + String.Format("{command:X02}"));
				line.AppendFormat(" Size:" + String.Format("{(int)(size - 8):D02}"));
				line.AppendFormat(" -");
				for (uint i = 0; i < size - 8; i++)
					line.AppendFormat(String.Format(" {0:X02}", (uint)(bytes[i])));

				LogFormat(line.ToString());
			}
		}
#endif

		/// <summary>
		/// Called when vJoy has a new FFB packet.
		/// WARNING This is called from a thread pool managed by windows.
		/// The thread itself is created and managed by vJoyInterface.dll.
		/// Overloading it will miss third party application FFB packets.
		/// </summary>
		/// <param name="ffbDataPtr"></param>
		/// <param name="userData"></param>
		public void FfbFunction1(IntPtr data, object userdata)
		{
			// Packet Header
			//copy ffb packet to managed structure
//		  InternalFfbPacket packet = (InternalFfbPacket)Marshal.PtrToStructure(data, typeof(InternalFfbPacket));

			// Packet Header
			LogFormat("============= FFB Packet =============");

			/////// Packet Device ID, and Type Block Index (if exists)
		#region Packet Device ID, and Type Block Index


			uint DeviceID = 0, BlockIndex = 0;
			FFBPType Type = new FFBPType();

			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_DeviceID(data, ref DeviceID)) {
				LogFormat(" > Device ID: {0}", DeviceID);
			}

			// Effect block index only used when simultaneous effects should be done by
			// underlying hardware, which is not the case for a single motor driving wheel
			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_EffectBlockIndex(data, ref BlockIndex)) {
				LogFormat(" > Effect Block Index: {0}", BlockIndex);
			}

			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_Type(data, ref Type)) {
				if (!PacketType2Str(Type, out var TypeStr))
					LogFormat(" > Packet Type: {0}", Type);
				else
					LogFormat(" > Packet Type: {0}", TypeStr);
				switch (Type) {
					case FFBPType.PT_POOLREP:
						LogFormat(" > Pool report handled by driver side");
						break;
					case FFBPType.PT_BLKLDREP:
						LogFormat(" > Block Load report handled by driver side");
						break;
					case FFBPType.PT_BLKFRREP:
						//FFBManager.FreeEffect(BlockIndex);
						// Update PID
						Joystick.FfbReadPID(DeviceID, ref PIDBlock);
						LogFormat(" > Block Free effect id {0}", PIDBlock.NextFreeEID);
						break;
				}
			}
		#endregion

		#region PID Device Control
		FFB_CTRL Control = new FFB_CTRL();
			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_DevCtrl(data, ref Control) && DevCtrl2Str(Control, out var CtrlStr)) {
				LogFormat(" >> PID Device Control: {0}", CtrlStr);
				switch (Control) {
					case FFB_CTRL.CTRL_DEVRST:
						// Update PID data to get the resetted values from driver side
						Joystick.FfbReadPID(DeviceID, ref PIDBlock);
						// device reset
						break;
					case FFB_CTRL.CTRL_ENACT:
						break;
					case FFB_CTRL.CTRL_DISACT:
						break;
					case FFB_CTRL.CTRL_STOPALL:
						break;
				}
			}
		#endregion

		#region Create new effect
			FFBEType EffectType = new FFBEType();
			uint NewBlockIndex = 0;
			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_CreateNewEffect(data, ref EffectType, ref NewBlockIndex)) {
				// Create new effect

				// Update PID
				Joystick.FfbReadPID(Id, ref PIDBlock);

				if (EffectType2Str(EffectType, out var TypeStr))
					LogFormat(" >> Effect Type: {0}", TypeStr);
				else
					LogFormat(" >> Effect Type: Unknown({0})", EffectType);
				LogFormat(" >> New Effect ID: {0}", NewBlockIndex);
				if (NewBlockIndex != PIDBlock.PIDBlockLoad.EffectBlockIndex) {
					LogFormat("!!! BUG NewBlockIndex=" + NewBlockIndex + " <> pid=" + ((int)PIDBlock.PIDBlockLoad.EffectBlockIndex));
				}
				LogFormat(" >> LoadStatus {0}", PIDBlock.PIDBlockLoad.LoadStatus);
			}
		#endregion

		#region Condition
			vJoy.FFB_EFF_COND Condition = new vJoy.FFB_EFF_COND();
			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_Eff_Cond(data, ref Condition)) {

				if (Condition.isY)
					LogFormat(" >> Y Axis");
				else
					LogFormat(" >> X Axis");
				LogFormat(" >> Center Point Offset: {0}", TwosCompWord2Int(Condition.CenterPointOffset));
				LogFormat(" >> Positive Coefficient: {0}", TwosCompWord2Int(Condition.PosCoeff));
				LogFormat(" >> Negative Coefficient: {0}", TwosCompWord2Int(Condition.NegCoeff));
				LogFormat(" >> Positive Saturation: {0}", Condition.PosSatur);
				LogFormat(" >> Negative Saturation: {0}", Condition.NegSatur);
				LogFormat(" >> Dead Band: {0}", Condition.DeadBand);

			}
		#endregion

		#region Effect Report
			vJoy.FFB_EFF_REPORT Effect = new vJoy.FFB_EFF_REPORT();
			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_Eff_Report(data, ref Effect)) {
				if (!EffectType2Str(Effect.EffectType, out var TypeStr))
					LogFormat(" >> Effect Report: {0} {1}", (int)Effect.EffectType, Effect.EffectType.ToString());
				else
					LogFormat(" >> Effect Report: {0}", TypeStr);
				LogFormat(" >> AxisEnabledDirection: {0}", (ushort)Effect.AxesEnabledDirection);
				if (Effect.Polar) {
					LogFormat(" >> Direction: {0} deg ({1})", Polar2Deg(Effect.Direction), Effect.Direction);
				} else {
					LogFormat(" >> X Direction: {0}", Effect.DirX);
					LogFormat(" >> Y Direction: {0}", Effect.DirY);
				}

				if (Effect.Duration == 0xFFFF)
					LogFormat(" >> Duration: Infinit");
				else
					LogFormat(" >> Duration: {0} MilliSec", (int)(Effect.Duration));

				if (Effect.TrigerRpt == 0xFFFF)
					LogFormat(" >> Trigger Repeat: Infinit");
				else
					LogFormat(" >> Trigger Repeat: {0}", (int)(Effect.TrigerRpt));

				if (Effect.SamplePrd == 0xFFFF)
					LogFormat(" >> Sample Period: Infinit");
				else
					LogFormat(" >> Sample Period: {0}", (int)(Effect.SamplePrd));

				if (Effect.StartDelay == 0xFFFF)
					LogFormat(" >> Start Delay: max ");
				else
					LogFormat(" >> Start Delay: {0}", (int)(Effect.StartDelay));


				LogFormat(" >> Gain: {0}%%", Byte2Percent(Effect.Gain));

			}
		#endregion

		#region Effect Operation
			vJoy.FFB_EFF_OP Operation = new vJoy.FFB_EFF_OP();
			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_EffOp(data, ref Operation) && EffectOpStr(Operation.EffectOp, out var EffOpStr))
			{
				LogFormat(" >> Effect Operation: {0}", EffOpStr);
				if (Operation.LoopCount == 0xFF)
					LogFormat(" >> Loop until stopped");
				else
					LogFormat(" >> Loop {0} times", (int)(Operation.LoopCount));

				switch (Operation.EffectOp) {
					case FFBOP.EFF_START:
						// Start the effect identified by the Effect Handle.
						break;
					case FFBOP.EFF_STOP:
						// Stop the effect identified by the Effect Handle.
						break;
					case FFBOP.EFF_SOLO:
						// Start the effect identified by the Effect Handle and stop all other effects.
						break;
				}
			}
		#endregion

		#region Global Device Gain
			byte Gain = 0;
			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_DevGain(data, ref Gain))
				LogFormat(" >> Global Device Gain: {0}", Byte2Percent(Gain));
		#endregion

		#region Envelope
			vJoy.FFB_EFF_ENVLP Envelope = new vJoy.FFB_EFF_ENVLP();
			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_Eff_Envlp(data, ref Envelope))
			{
				LogFormat(" >> Attack Level: {0}", Envelope.AttackLevel);
				LogFormat(" >> Fade Level: {0}", Envelope.FadeLevel);
				LogFormat(" >> Attack Time: {0}", (int)(Envelope.AttackTime));
				LogFormat(" >> Fade Time: {0}", (int)(Envelope.FadeTime));
			}
		#endregion

		#region Periodic
			vJoy.FFB_EFF_PERIOD EffPrd = new vJoy.FFB_EFF_PERIOD();
			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_Eff_Period(data, ref EffPrd))
			{
				LogFormat(" >> Magnitude: {0}", EffPrd.Magnitude);
				LogFormat(" >> Offset: {0}", TwosCompWord2Int(EffPrd.Offset));
				LogFormat(" >> Phase: {0}", EffPrd.Phase * 3600 / 255);
				LogFormat(" >> Period: {0}", (int)(EffPrd.Period));
			}
		#endregion

		#region Ramp Effect
			vJoy.FFB_EFF_RAMP RampEffect = new vJoy.FFB_EFF_RAMP();
			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_Eff_Ramp(data, ref RampEffect))
			{
				LogFormat(" >> Ramp Start: {0}", TwosCompWord2Int(RampEffect.Start));
				LogFormat(" >> Ramp End: {0}", TwosCompWord2Int(RampEffect.End));
			}
		#endregion

		#region Constant Effect
			vJoy.FFB_EFF_CONSTANT CstEffect = new vJoy.FFB_EFF_CONSTANT();
			if ((uint)ERROR.ERROR_SUCCESS == Joystick.Ffb_h_Eff_Constant(data, ref CstEffect)) {
				LogFormat(" >> Block Index: {0}", TwosCompWord2Int(CstEffect.EffectBlockIndex));
				LogFormat(" >> Magnitude: {0}", TwosCompWord2Int(CstEffect.Magnitude));
			}
		#endregion

//		  DumpFrame(data);
			LogFormat("======================================");
		}		// FfbFunction1()
#endif // FFB

		// Convert Packet type to String
		public static bool PacketType2Str(FFBPType Type, out string Str)
		{
			bool stat = true;
			Str = "";

			switch (Type) {
				case FFBPType.PT_EFFREP:
					Str = "Effect Report";
					break;
				case FFBPType.PT_ENVREP:
					Str = "Envelope Report";
					break;
				case FFBPType.PT_CONDREP:
					Str = "Condition Report";
					break;
				case FFBPType.PT_PRIDREP:
					Str = "Periodic Report";
					break;
				case FFBPType.PT_CONSTREP:
					Str = "Constant Force Report";
					break;
				case FFBPType.PT_RAMPREP:
					Str = "Ramp Force Report";
					break;
				case FFBPType.PT_CSTMREP:
					Str = "Custom Force Data Report";
					break;
				case FFBPType.PT_SMPLREP:
					Str = "Download Force Sample";
					break;
				case FFBPType.PT_EFOPREP:
					Str = "Effect Operation Report";
					break;
				case FFBPType.PT_BLKFRREP:
					Str = "PID Block Free Report";
					break;
				case FFBPType.PT_CTRLREP:
					Str = "PID Device Control";
					break;
				case FFBPType.PT_GAINREP:
					Str = "Device Gain Report";
					break;
				case FFBPType.PT_SETCREP:
					Str = "Set Custom Force Report";
					break;
				case FFBPType.PT_NEWEFREP:
					Str = "Create New Effect Report";
					break;
				case FFBPType.PT_BLKLDREP:
					Str = "Block Load Report";
					break;
				case FFBPType.PT_POOLREP:
					Str = "PID Pool Report";
					break;
				default:
					stat = false;
					break;
			}
			return stat;
		}		// PacketType2Str()

		// Convert Effect type to String
		public static bool EffectType2Str(FFBEType Type, out string Str)
		{
			bool stat = true;
			Str = "";

			switch (Type) {
				case FFBEType.ET_NONE:
					stat = false;
					break;
				case FFBEType.ET_CONST:
					Str = "Constant Force";
					break;
				case FFBEType.ET_RAMP:
					Str = "Ramp";
					break;
				case FFBEType.ET_SQR:
					Str = "Square";
					break;
				case FFBEType.ET_SINE:
					Str = "Sine";
					break;
				case FFBEType.ET_TRNGL:
					Str = "Triangle";
					break;
				case FFBEType.ET_STUP:
					Str = "Sawtooth Up";
					break;
				case FFBEType.ET_STDN:
					Str = "Sawtooth Down";
					break;
				case FFBEType.ET_SPRNG:
					Str = "Spring";
					break;
				case FFBEType.ET_DMPR:
					Str = "Damper";
					break;
				case FFBEType.ET_INRT:
					Str = "Inertia";
					break;
				case FFBEType.ET_FRCTN:
					Str = "Friction";
					break;
				case FFBEType.ET_CSTM:
					Str = "Custom Force";
					break;
				default:
					stat = false;
					break;
			}
			return stat;
		}
		// Convert PID Device Control to String

		public static bool DevCtrl2Str(FFB_CTRL Ctrl, out string Str)
		{
			bool stat = true;
			Str = "";

			switch (Ctrl) {
				case FFB_CTRL.CTRL_ENACT:
					Str = "Enable Actuators";
					break;
				case FFB_CTRL.CTRL_DISACT:
					Str = "Disable Actuators";
					break;
				case FFB_CTRL.CTRL_STOPALL:
					Str = "Stop All Effects";
					break;
				case FFB_CTRL.CTRL_DEVRST:
					Str = "Device Reset";
					break;
				case FFB_CTRL.CTRL_DEVPAUSE:
					Str = "Device Pause";
					break;
				case FFB_CTRL.CTRL_DEVCONT:
					Str = "Device Continue";
					break;
				default:
					stat = false;
					break;
			}
			return stat;
		}

		// Convert Effect operation to string
		public static bool EffectOpStr(FFBOP Op, out string Str)
		{
			bool stat = true;
			Str = "";

			switch (Op) {
				case FFBOP.EFF_START:
					Str = "Effect Start";
					break;
				case FFBOP.EFF_SOLO:
					Str = "Effect Solo Start";
					break;
				case FFBOP.EFF_STOP:
					Str = "Effect Stop";
					break;
				default:
					stat = false;
					break;
			}
			return stat;
		}

		// Polar values (0x00-0xFF) to Degrees (0-360)
		public static int Polar2Deg(System.UInt16 Polar)
		{
			return (int)((long)Polar * 360) / 32767;
		}

		// Convert range 0x00-0xFF to 0%-100%
		public static int Byte2Percent(byte InByte)
		{
			return ((byte)InByte * 100) / 255;
		}

		// Convert One-Byte 2's complement input to integer
		public static int TwosCompByte2Int(byte inb)
		{
			int tmp;
			byte inv = (byte)~inb;
			bool isNeg = ((inb >> 7) != 0);
			if (isNeg) {
				tmp = (int)(inv);
				tmp = -1 * tmp;
				return tmp;
			} else return (int)inb;
		}

		// Convert One-Byte 2's complement input to integer
		public static int TwosCompWord2Int(short inb)
		{
			int tmp;
			int inv = (int)~inb + 1;
			bool isNeg = ((inb >> 15) != 0);
			if (isNeg) {
				tmp = (int)(inv);
				tmp = -1 * tmp;
				return tmp;
			} else return (int)inb;
		}
	}		// class VJoyFFBReceiver
}
