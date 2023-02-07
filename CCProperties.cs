using System;
using System.Linq;
using SimHub.Plugins;

namespace blekenbleu.MIDIspace
{
    // Dynamically add events and properties for CC buttons as pressed
    // Working around the SimHub limitation that AttachDelegate() fails for variables.
    internal class CCProperties
    {
        internal string[] send;		// configuration by NCalc script
        internal string[,] Send;			// these are used at 60Hz
        internal string[] CCname;			// for AttachDelegate()
        internal readonly byte unconfigured = 16, CC = 1, Button = 2, VJax = 4, VJbut = 8;
       	internal byte[] CCvalue;			// store CC values
        internal byte[] SendCt, MySendCt;
       	internal byte[] Which, Unmap;	// remap Sends and configured CC number assigned types
        internal byte[,] Map;
        private byte[] Configured;			// temporarily accumulate configured Sends
        private string[]  Ping;				// ping[0-7]
        private string[,] prop;

	// VJD.Init() is already run; sort "my" properties first to send, even when game is not running
        internal void Init(MIDIio I, byte[] Size)
        {
            send = new string[] { I.Ini + "send", I.Ini + "VJDbutton", I.Ini + "VJDaxis"};
            prop = new string[send.Length, Size[0]];	// accumulate 'send' entries in the order received
            byte ct;
            Map = new byte[send.Length, Size[0]];	// first MySendCt entries will be for I.my properties
            SendCt = new byte[send.Length];
            MySendCt = new byte[send.Length];
            Which = new byte[128];			// OUTwetdry.Init() resends unconfigured CCs on restart if DoEcho && (0 < unconfigured & Which[i])
            Unmap = new byte[128];			// OnEventSent() warns of unexpected CC numbers sent
            CCvalue = new byte[128];
            CCname = new string[128];

            for (byte i = ct = 0; i < 128; i++)		// extract unconfigured CC flags
            {
                if (0 < (0x80 & I.Settings.Sent[i]))
                {
                    ct++;
                    I.Settings.Sent[i] &= 0x7F;
                    Which[i] = unconfigured;
                }
                else Which[i] = 0;
                CCvalue[i] = 0;
                Unmap[i] = i;
                CCname[i] = "CC" + i;
            }
            I.Log(4, $"{I.My}CCProperties.Init():  {ct} unconfigured CCs");

            Send = new string[send.Length, Size[0]];		// these will be used at 60Hz
            Configured = new byte[Size[0]];		// temporarily acumulate configured Sends
            bool[,] ok = new bool[send.Length,Size[0]];	// valid send properties non I.my
            Ping = new string[Size[0]];
            for (byte i = 0; i < Size[0]; i++)
                Ping[i] = "ping" + i;

            for (byte j = 0; j < send.Length; j++)
            {
                MySendCt[j] = 0;
                for (byte i = 0; i < Size[j]; i++)   	// snag 'My' configured sends
                {
                    prop[j, i] = I.PluginManager.GetPropertyValue($"{send[j]}{i}")?.ToString();
                    if (ok[j, i] = (null != prop[j, i] && 0 < prop[j, i].Length))
                        // test for properties with "my" I.My prefix
                        if ((3 + I.My.Length) < prop[j, i].Length && I.My == (prop[j, i].Substring(0, I.My.Length)))
                        {
                            I.Log(4, I.My + "CCProperties(): '{prop[i].Substring(0, I.my.Length)}' == '{I.my.Length}'  ");
                            ok[j, i] = false;
                            Send[j, MySendCt[j]] = prop[j, i];
                            MySendCt[j]++;         		// configured I.my CC's get echo'ed
                        }
                }

                SendCt[j] = MySendCt[j];			// first MySendCt entries are indices for "My" outputs
                for (byte i = 0; i < Size[j]; i++)   		// now, configured sends NOT from I.my.send[n < MySendCt]
                {
                    if (ok[j, i])				// not already assigned to Send[,]?
                    {
                        if (0 == j)
                            Configured[SendCt[0]] = i;		// find a CC number to reuse for this send
                        Send[j, SendCt[j]++] = prop[j, i];
                    }
                }
            }
        }	// Init()

        internal void End(MIDIio I)
        {
            byte ct; 

            for (byte i = ct = 0; i < 128; i++)
                if (4 == Which[i])
                {
                    I.Settings.Sent[i] |= 0x80; // flag unconfigured CCs to restore
                    ct++;
                }
            I.Log(4, $"{I.My}Properties.End():  {ct} unconfigured CCs");
        }

        private void Action(MIDIio I, byte bn, byte CCnumber)
        {
            I.AddEvent(CCname[CCnumber]);
            switch (bn)				// configure button property and event
            {
                case 0:
                    I.AddAction(Ping[0],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)0));
                    break;
                case 1:
                    I.AddAction(Ping[1],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)1));
                    break;
                case 2:
                    I.AddAction(Ping[2],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)2));
                    break;
                case 3:
                    I.AddAction(Ping[3],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)3));
                    break;
                case 4:
                    I.AddAction(Ping[4],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)4));
                    break;
                case 5:
                    I.AddAction(Ping[5],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)5));
                    break;
                case 6:
                    I.AddAction(Ping[6],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)6));
                    break;
                case 7:
                    I.AddAction(Ping[7],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)7));
                    break;
                default:
                    I.Info($"{I.My}Action(): invalid Ping[{bn}] for {CCnumber}");
                    break;
              }
        }

        internal bool SetProp(MIDIio I, int CCnumber, byte value)
        {
            switch (CCnumber)       // configure CC property and event
            {
                case 0:
                    I.AttachDelegate(CCname[0], () => CCvalue[0]);
                    break;
                case 1:
                    I.AttachDelegate(CCname[1], () => CCvalue[1]);
                    break;
                case 2:
                    I.AttachDelegate(CCname[2], () => CCvalue[2]);
                    break;
                case 3:
                    I.AttachDelegate(CCname[3], () => CCvalue[3]);
                    break;
                case 4:
                    I.AttachDelegate(CCname[4], () => CCvalue[4]);
                    break;
                case 5:
                    I.AttachDelegate(CCname[5], () => CCvalue[5]);
                    break;
                case 6:
                    I.AttachDelegate(CCname[6], () => CCvalue[6]);
                    break;
                case 7:
                    I.AttachDelegate(CCname[7], () => CCvalue[7]);
                    break;
                case 8:
                    I.AttachDelegate(CCname[8], () => CCvalue[8]);
                    break;
                case 9:
                    I.AttachDelegate(CCname[9], () => CCvalue[9]);
                    break;
                case 10:
                    I.AttachDelegate(CCname[10], () => CCvalue[10]);
                    break;
                case 11:
                    I.AttachDelegate(CCname[11], () => CCvalue[11]);
                    break;
                case 12:
                    I.AttachDelegate(CCname[12], () => CCvalue[12]);
                    break;
                case 13:
                    I.AttachDelegate(CCname[13], () => CCvalue[13]);
                    break;
                case 14:
                    I.AttachDelegate(CCname[14], () => CCvalue[14]);
                    break;
                case 15:
                    I.AttachDelegate(CCname[15], () => CCvalue[15]);
                    break;
                case 16:
                    I.AttachDelegate(CCname[16], () => CCvalue[16]);
                    break;
                case 17:
                    I.AttachDelegate(CCname[17], () => CCvalue[17]);
                    break;
                case 18:
                    I.AttachDelegate(CCname[18], () => CCvalue[18]);
                    break;
                case 19:
                    I.AttachDelegate(CCname[19], () => CCvalue[19]);
                    break;
                case 20:
                    I.AttachDelegate(CCname[20], () => CCvalue[20]);
                    break;
                case 21:
                    I.AttachDelegate(CCname[21], () => CCvalue[21]);
                    break;
                case 22:
                    I.AttachDelegate(CCname[22], () => CCvalue[22]);
                    break;
                case 23:
                    I.AttachDelegate(CCname[23], () => CCvalue[23]);
                    break;
                case 24:
                    I.AttachDelegate(CCname[24], () => CCvalue[24]);
                    break;
                case 25:
                    I.AttachDelegate(CCname[25], () => CCvalue[25]);
                    break;
                case 26:
                    I.AttachDelegate(CCname[26], () => CCvalue[26]);
                    break;
                case 27:
                    I.AttachDelegate(CCname[27], () => CCvalue[27]);
                    break;
                case 28:
                    I.AttachDelegate(CCname[28], () => CCvalue[28]);
                    break;
                case 29:
                    I.AttachDelegate(CCname[29], () => CCvalue[29]);
                    break;
                case 30:
                    I.AttachDelegate(CCname[30], () => CCvalue[30]);
                    break;
                case 31:
                    I.AttachDelegate(CCname[31], () => CCvalue[31]);
                    break;
                case 32:
                    I.AttachDelegate(CCname[32], () => CCvalue[32]);
                    break;
                case 33:
                    I.AttachDelegate(CCname[33], () => CCvalue[33]);
                    break;
                case 34:
                    I.AttachDelegate(CCname[34], () => CCvalue[34]);
                    break;
                case 35:
                    I.AttachDelegate(CCname[35], () => CCvalue[35]);
                    break;
                case 36:
                    I.AttachDelegate(CCname[36], () => CCvalue[36]);
                    break;
                case 37:
                    I.AttachDelegate(CCname[37], () => CCvalue[37]);
                    break;
                case 38:
                    I.AttachDelegate(CCname[38], () => CCvalue[38]);
                    break;
                case 39:
                    I.AttachDelegate(CCname[39], () => CCvalue[39]);
                    break;
                case 40:
                    I.AttachDelegate(CCname[40], () => CCvalue[40]);
                    break;
                case 41:
                    I.AttachDelegate(CCname[41], () => CCvalue[41]);
                    break;
                case 42:
                    I.AttachDelegate(CCname[42], () => CCvalue[42]);
                    break;
                case 43:
                    I.AttachDelegate(CCname[43], () => CCvalue[43]);
                    break;
                case 44:
                    I.AttachDelegate(CCname[44], () => CCvalue[44]);
                    break;
                case 45:
                    I.AttachDelegate(CCname[45], () => CCvalue[45]);
                    break;
                case 46:
                    I.AttachDelegate(CCname[46], () => CCvalue[46]);
                    break;
                case 47:
                    I.AttachDelegate(CCname[47], () => CCvalue[47]);
                    break;
                case 48:
                    I.AttachDelegate(CCname[48], () => CCvalue[48]);
                    break;
                case 49:
                    I.AttachDelegate(CCname[49], () => CCvalue[49]);
                    break;
                case 50:
                    I.AttachDelegate(CCname[50], () => CCvalue[50]);
                    break;
                case 51:
                    I.AttachDelegate(CCname[51], () => CCvalue[51]);
                    break;
                case 52:
                    I.AttachDelegate(CCname[52], () => CCvalue[52]);
                    break;
                case 53:
                    I.AttachDelegate(CCname[53], () => CCvalue[53]);
                    break;
                case 54:
                    I.AttachDelegate(CCname[54], () => CCvalue[54]);
                    break;
                case 55:
                    I.AttachDelegate(CCname[55], () => CCvalue[55]);
                    break;
                case 56:
                    I.AttachDelegate(CCname[56], () => CCvalue[56]);
                    break;
                case 57:
                    I.AttachDelegate(CCname[57], () => CCvalue[57]);
                    break;
                case 58:
                    I.AttachDelegate(CCname[58], () => CCvalue[58]);
                    break;
                case 59:
                    I.AttachDelegate(CCname[59], () => CCvalue[59]);
                    break;
                case 60:
                    I.AttachDelegate(CCname[60], () => CCvalue[60]);
                    break;
                case 61:
                    I.AttachDelegate(CCname[61], () => CCvalue[61]);
                    break;
                case 62:
                    I.AttachDelegate(CCname[62], () => CCvalue[62]);
                    break;
                case 63:
                    I.AttachDelegate(CCname[63], () => CCvalue[63]);
                    break;
                case 64:
                    I.AttachDelegate(CCname[64], () => CCvalue[64]);
                    break;
                case 65:
                    I.AttachDelegate(CCname[65], () => CCvalue[65]);
                    break;
                case 66:
                    I.AttachDelegate(CCname[66], () => CCvalue[66]);
                    break;
                case 67:
                    I.AttachDelegate(CCname[67], () => CCvalue[67]);
                    break;
                case 68:
                    I.AttachDelegate(CCname[68], () => CCvalue[68]);
                    break;
                case 69:
                    I.AttachDelegate(CCname[69], () => CCvalue[69]);
                    break;
                case 70:
                    I.AttachDelegate(CCname[70], () => CCvalue[70]);
                    break;
                case 71:
                    I.AttachDelegate(CCname[71], () => CCvalue[71]);
                    break;
                case 72:
                    I.AttachDelegate(CCname[72], () => CCvalue[72]);
                    break;
                case 73:
                    I.AttachDelegate(CCname[73], () => CCvalue[73]);
                    break;
                case 74:
                    I.AttachDelegate(CCname[74], () => CCvalue[74]);
                    break;
                case 75:
                    I.AttachDelegate(CCname[75], () => CCvalue[75]);
                    break;
                case 76:
                    I.AttachDelegate(CCname[76], () => CCvalue[76]);
                    break;
                case 77:
                    I.AttachDelegate(CCname[77], () => CCvalue[77]);
                    break;
                case 78:
                    I.AttachDelegate(CCname[78], () => CCvalue[78]);
                    break;
                case 79:
                    I.AttachDelegate(CCname[79], () => CCvalue[79]);
                    break;
                case 80:
                    I.AttachDelegate(CCname[80], () => CCvalue[80]);
                    break;
                case 81:
                    I.AttachDelegate(CCname[81], () => CCvalue[81]);
                    break;
                case 82:
                    I.AttachDelegate(CCname[82], () => CCvalue[82]);
                    break;
                case 83:
                    I.AttachDelegate(CCname[83], () => CCvalue[83]);
                    break;
                case 84:
                    I.AttachDelegate(CCname[84], () => CCvalue[84]);
                    break;
                case 85:
                    I.AttachDelegate(CCname[85], () => CCvalue[85]);
                    break;
                case 86:
                    I.AttachDelegate(CCname[86], () => CCvalue[86]);
                    break;
                case 87:
                    I.AttachDelegate(CCname[87], () => CCvalue[87]);
                    break;
                case 88:
                    I.AttachDelegate(CCname[88], () => CCvalue[88]);
                    break;
                case 89:
                    I.AttachDelegate(CCname[89], () => CCvalue[89]);
                    break;
                case 90:
                    I.AttachDelegate(CCname[90], () => CCvalue[90]);
                    break;
                case 91:
                    I.AttachDelegate(CCname[91], () => CCvalue[91]);
                    break;
                case 92:
                    I.AttachDelegate(CCname[92], () => CCvalue[92]);
                    break;
                case 93:
                    I.AttachDelegate(CCname[93], () => CCvalue[93]);
                    break;
                case 94:
                    I.AttachDelegate(CCname[94], () => CCvalue[94]);
                    break;
                case 95:
                    I.AttachDelegate(CCname[95], () => CCvalue[95]);
                    break;
                case 96:
                    I.AttachDelegate(CCname[96], () => CCvalue[96]);
                    break;
                case 97:
                    I.AttachDelegate(CCname[97], () => CCvalue[97]);
                    break;
                case 98:
                    I.AttachDelegate(CCname[98], () => CCvalue[98]);
                    break;
                case 99:
                    I.AttachDelegate(CCname[99], () => CCvalue[99]);
                    break;
                case 100:
                    I.AttachDelegate(CCname[100], () => CCvalue[100]);
                    break;
                case 101:
                    I.AttachDelegate(CCname[101], () => CCvalue[101]);
                    break;
                case 102:
                    I.AttachDelegate(CCname[102], () => CCvalue[102]);
                    break;
                case 103:
                    I.AttachDelegate(CCname[103], () => CCvalue[103]);
                    break;
                case 104:
                    I.AttachDelegate(CCname[104], () => CCvalue[104]);
                    break;
                case 105:
                    I.AttachDelegate(CCname[105], () => CCvalue[105]);
                    break;
                case 106:
                    I.AttachDelegate(CCname[106], () => CCvalue[106]);
                    break;
                case 107:
                    I.AttachDelegate(CCname[107], () => CCvalue[107]);
                    break;
                case 108:
                    I.AttachDelegate(CCname[108], () => CCvalue[108]);
                    break;
                case 109:
                    I.AttachDelegate(CCname[109], () => CCvalue[109]);
                    break;
                case 110:
                    I.AttachDelegate(CCname[110], () => CCvalue[110]);
                    break;
                case 111:
                    I.AttachDelegate(CCname[111], () => CCvalue[111]);
                    break;
                case 112:
                    I.AttachDelegate(CCname[112], () => CCvalue[112]);
                    break;
                case 113:
                    I.AttachDelegate(CCname[113], () => CCvalue[113]);
                    break;
                case 114:
                    I.AttachDelegate(CCname[114], () => CCvalue[114]);
                    break;
                case 115:
                    I.AttachDelegate(CCname[115], () => CCvalue[115]);
                    break;
                case 116:
                    I.AttachDelegate(CCname[116], () => CCvalue[116]);
                    break;
                case 117:
                    I.AttachDelegate(CCname[117], () => CCvalue[117]);
                    break;
                case 118:
                    I.AttachDelegate(CCname[118], () => CCvalue[118]);
                    break;
                case 119:
                    I.AttachDelegate(CCname[119], () => CCvalue[119]);
                    break;
                case 120:
                    I.AttachDelegate(CCname[120], () => CCvalue[120]);
                    break;
                case 121:
                    I.AttachDelegate(CCname[121], () => CCvalue[121]);
                    break;
                case 122:
                    I.AttachDelegate(CCname[122], () => CCvalue[122]);
                    break;
                case 123:
                    I.AttachDelegate(CCname[123], () => CCvalue[123]);
                    break;
                case 124:
                    I.AttachDelegate(CCname[124], () => CCvalue[124]);
                    break;
                case 125:
                    I.AttachDelegate(CCname[125], () => CCvalue[125]);
                    break;
                case 126:
                    I.AttachDelegate(CCname[126], () => CCvalue[126]);
                    break;
                case 127:
                    I.AttachDelegate(CCname[127], () => CCvalue[127]);
                    break;
                default:
                    I.Info($"{I.My}SetProp() not set: CC{CCnumber}");
                    return false;
            }
            CCvalue[CCnumber] = value;
            return true;
        }	// SetProp()

        private byte mc = 0;					// Configured[] index count
        // Init() identified valid send entries and sorted prop[] names into Send[] with I.my first

        internal void Attach(MIDIio I)	// call SetProp to AttachDelegate() MIDIin properties based on ExternalScript.MIDI* properties
        {
            string[] setting = { "unconfigured", "slider", "knob", "button" };	// Which types
            byte[] Wmap = new byte[] { unconfigured, CC, CC, Button };	        // Which type flag bits
            string send = I.Ini + "out";
            int L = I.My.Length;
            byte my = 0;

            if (I.Log(4, I.My + "Attach() my Send:"))
            {
                for (byte j = 0; j < Send.GetLength(0); j++)
                    for (byte i = 0; i < MySendCt[j]; i++)
                        I.Info("\t" + Send[j, i] + " AKA " + Send[j, i].Substring(L, Send[j, i].Length - L));
            }

            for (byte s = 1; s < setting.Length; s++)		// reserve s == 0 for sends and unassigned CCs
            {
                string type = I.Ini + setting[s] + 's';
                string value = I.PluginManager.GetPropertyValue(type)?.ToString();
                if (null == value) {
                    I.Info($"{I.My}Attach(): '{type}' not found");
                    continue;
                }

                // bless the Internet
                byte[] array = value.Split(',').Select(byte.Parse).ToArray();
                I.Log(4, $"{I.My}Attach(): '{I.Ini + setting[s]}' {string.Join(",", array.Select(p => p.ToString()).ToArray())}");

                byte cn = 0;                 			// index settings[]
                foreach (byte cc in array)		        // array has cc numbers assigned for this type
                {
                    bool first = (0 == Which[cc]);
                    if (first)					// CCname[cc] may have been already been assigned for MIDI
                    {
                        CCname[cc] = setting[s] + cn;       	// lacks I.Ini

	                I.Log(4, $"{I.My}Attach():  CCname[{cc}] = {CCname[cc]}");
                        for (byte mi = 0; mi < MySendCt[0]; mi++)
                            if (CCname[cc].Length == (Send[0, mi].Length - L) && CCname[cc] == Send[0, mi].Substring(L, CCname[cc].Length))
                            {
                                Unmap[cc] = my;
                                Map[0, my++] = cc;			// reuse this input CC number for output
                            }
                            else if (mc < I.Size[0])
                                Configured[mc++] = cc;

 	                SetProp(I, cc, I.Settings.Sent[cc]);	// set property for newly configured input
                        if (Button == Wmap[s])
                        {
			    Which[cc] |= Button;
                            Action(I, cn, cc);
                        }
                    }
                    else I.Log(4, $"{setting[s] + cn} previously configured as {CCname[cc]}");
                    cn++;					// next setting[s]
                }
            }
            // MIDIin property configuration is now complete with mc CC numbers available in Configured[] for MIDIout not mine
            // if configured input CC number count < configured outputs,
            // then configured output numbers potentially collide with low unconfigured input CC numbers
            if (mc < SendCt[0])
                I.Info($"{I.My}Attach(): {mc} allocated MIDIin count < {SendCt} for MIDIout");
            if (my < MySendCt[0])
                I.Info($"{I.My}Attach(): {MySendCt[0]-my}/{MySendCt[0]} MySendCt[0] were NOT configured");

            byte ct;
            if (!I.DoEcho)
                for (ct = 0; ct < 128; ct++)
                    if (0 < (unconfigured & Which[ct]))
                        SetProp(I, ct, I.Settings.Sent[ct]);	// restore previous received unconfigured CCs

            ct = MySendCt[0];
            for (my = 0; my < mc && ct < SendCt[0]; my++) 
                Map[0, ct++] = Configured[my];			// recycle configured MIDIin CC numbers

            // appropriate previously unallocated CC numbers;  those controls will be unavailable in DeEcho mode..
            for (my = 127; MySendCt[0] <= my && ct < SendCt[0]; my--)	// configure CC numbers for MySendCt <= i < SendCt
            {
                if (0 == Which[my])
                {
                    Map[0, ct++] = my;				// appropriate unconfigured MIDIin CC numbers
                    Which[my] = unconfigured;			// set my as appropriated
                }
            }
            if (ct < SendCt[0])
                I.Info($"{I.My}Attach(): '{prop[0, ct]}' not Map[]ed");
        }	// Attach()
    }
}
