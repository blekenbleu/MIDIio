using System;
using System.Linq;
using SimHub.Plugins;

namespace blekenbleu.MIDIspace
{
    // Dynamically add events and properties for CC buttons as pressed
    // Working around the SimHub limitation that AttachDelegate() fails for variables.
    internal class IOproperties
    {
	internal string[][] Send, JoyName;				// these properties may be sent at 60Hz
	private string[] SendType;					// configuration by NCalc script
	internal byte[,] SendCt, CCmap;					// cumulative SendType subtotals, map non-CC outputs to configured MIDIin CC numbers
	internal string[] CCname, CCtype;				// for AttachDelegate();  CCn gets replaced by slider/knob/button[0 to size-1]
	internal byte[] CCvalue, CCct, CCst, SrcCt;				// store CC values, count CCmap entries
	internal byte[] Which, Unmap;					// remap CC Sends; Active() test Which for non-zero (already active), Button and unconfigured
	internal readonly byte unconfigured = 4, CC = 1, Button = 2;	// flags for Which[]
	internal byte[][] Map, JoyMap;					// reorder SendProp refresh
	private byte[] Configured;					// temporarily accumulate configured Sends
	private string[]  Ping;						// ping[0-7]
	private string[,] SendProp;
	private byte[,] CCsend;						// collect configured MIDIout CCs to send

	private string Join_strings(string s, string[] vector, byte length)
	{
		string sout = vector[0];

	    for (byte i = 1; i < length; i++)
		sout += s + vector[i];
	    return sout;
	}

	private string Join_bytes(string s, byte[] vector, byte length)
	{
	    string sout = vector[0].ToString();
	    for (byte i = 1; i < length; i++)
		sout += s + vector[i].ToString();
	    return sout;
	}

	private byte Unique(byte ctr, string[] name, string snew)
	{
	    for (byte i = 0; i < name.Length; i++)
		if (name[i].Length == snew.Length && name[i] == snew)
		    return i;
	    return ++ctr;
	}

	private byte UniqueSend(byte b, string prop)			// returns index for unique Send[][] property name
	{
	    byte z;
	    if (SrcCt[b] < ( z = Unique(SrcCt[b], Send[b], prop)))
	    {
		Send[b][SrcCt[b]++] = prop;
		return SrcCt[b];
	    }
	    return z;
	}

	private byte UniqueCC(string prop)
	{
	    string prop5 = prop.Substring(0,5);
	    byte b = (byte)128;
			byte k;

	    for (k = 1; k < CCtype.Length; k++)
		if (prop5 == CCtype[k].Substring(0,5))
		    break;

	    if (k == CCtype.Length)
		return (MIDIio.Info("UniqueCC() unrecognized property:  " + prop)) ? b : b;

	    for (byte j = 0; j < CCst[k]; j++)			// search already configured CCs
		if (CCname[CCsend[k, j]] == prop)
		    return CCsend[k, j];

            for (byte j = 0; j < CCct[k]; j++)			// search thru configured MIDIin CCs for a match
		if (prop == CCname[CCmap[k, j]])
		    return CCmap[k, CCst[k]++] = CCmap[k, j];

            return (MIDIio.Info("UniqueCC() no match in configured MIDIin." + "CCtype[k] for " + prop)) ? b : b;  
	}

	// VJD.Init() has already run; now sort "my" CC properties first for sending, even when game is not running
	internal void Init(MIDIio I)
	{								// configuration property name prefixes
	    SendType = new string[] { "send", "vJDaxis", "vJDbutton" };
	    SendProp = new string[SendType.Length, MIDIio.size];	// accumulate property names in discovered order
	    SendCt = new byte[SendType.Length, 1 + MIDIio.Real.Length];	// SendCt entries sorted for SendType[], then game properties
	    Send = new string[][] {new string[MIDIio.Size[0]], new string[MIDIio.Size[1]], new string[MIDIio.Size[2]] }; // SendProp[][] sorted by SendCt[,]
	    Map = new byte[][] { new byte[128], new byte[MIDIio.Size[1]], new byte[MIDIio.Size[2]] };	// Send[][] indices sorted by SendCt[,]

	    JoyName = new string[][] { new string[MIDIio.Size[1]], new string[MIDIio.Size[2]] };
	    JoyMap = new byte[][] { new byte[MIDIio.Size[1]], new byte[MIDIio.Size[2]] };

	    CCtype = new string[] { "unconfigured", "slider", "knob", "button" };	// Which CC types
	    CCct = new byte[] {0, 0, 0, 0};				// count CCmap[] entries.
	    CCst = new byte[] {0, 0, 0, 0};				// count CC CCsend[][] entries.
	    SrcCt = new byte[] {0, 0, 0, 0};				// count other Send[][] entries.
	    CCmap = new byte[CCtype.Length, MIDIio.size];		// collect configured MIDIin CC numbers for Map[][]
	    CCsend = new byte[CCtype.Length, MIDIio.size];		// collect configured MIDIout properties
	    Which = new byte[128];					// OUTwetdry.Init() resends unconfigured CCs on restart
	    Unmap = new byte[128];					// OnEventSent() warns of unexpected CC numbers sent
	    CCname = new string[128];					// first CCname[SendCt[0, MIDIio.Real.Length]] entries will be Send[0]
	    CCvalue = new byte[128];					// first CCvalue[SendCt[0, MIDIio.Real.Length]] entries will be for Send[0]
	    byte ct, j;
	    Configured = new byte[MIDIio.size];				// temporarily acumulate configured Sends
	    Ping = new string[MIDIio.Size[0]];
	    for (j = 0; j < MIDIio.Size[0]; j++)
		Ping[j] = "ping" + j;


	    MIDIio.Log(8, "Properties.send.Length = " + SendType.Length);
	    for (j = ct = 0; j < 128; j++)				// extract unconfigured CC flags
	    {
		CCvalue[j] = 0;
		Unmap[j] = j;
		CCname[j] = "CC" + j;

		if (0 < (0x80 & I.Settings.Sent[j]))
		{
		    ct++;
		    I.Settings.Sent[j] &= 0x7F;
		    Which[j] = unconfigured;
		}
		else Which[j] = 0;
	    }

            if (MIDIio.DoEcho)
	    {
                for (byte i = j = 0; j < ct && i < 128; i++)                         // resend saved CCs
                {
                    if (0 < (MIDIio.Properties.unconfigured & MIDIio.Properties.Which[i]))  // unconfigured CC number?
                    {
                        I.Outer.SendCCval(i, I.Settings.Sent[i]);              // much time may have passed;  reinitialize MIDIout device
                        j++;
                    }
                }
	        MIDIio.Log(4, $"CCProperties.Init(): {j} CCs resent after restart");
	    } else MIDIio.Log(4, $"CCProperties.Init():  {ct} unconfigured CCs restored");

	    // these will be used at 60Hz

	    bool[,] ok = new bool[SendType.Length,MIDIio.size];			// valid Real send properties
	    bool[,] game = new bool[SendType.Length,MIDIio.size];
            for (ct = 0; ct < SendType.Length; ct++)
	    {
	    	SendCt[ct,0] = 0;
		for (j = 0; j < MIDIio.size; j++)
		    game[ct, j] = ! (ok[ct, j] = false);
	    }

	    // Collect MIDIin properties from MIDIio.ini
            for (ct = 1; ct < CCtype.Length; ct++)
	    {
		string type = MIDIio.Ini + CCtype[ct] + 's';
		string value = I.PluginManager.GetPropertyValue(type)?.ToString();
		if (null == value) {
		    MIDIio.Info($"Init(): '{type}' not found");
		    continue;
		}

		// bless the Internet
		byte[] array = value.Split(',').Select(byte.Parse).ToArray();
		MIDIio.Log(4, $"Init(): '{MIDIio.Ini + CCtype[ct]}' {string.Join(",", array.Select(p => p.ToString()).ToArray())}");

		foreach (byte cc in array)			// array has cc numbers assigned for this type
		{
		    for (byte k = 1; k <= ct; k++)
		    for (j = 0; j < CCct[k]; j++)		// search for duplicates
			if (cc == CCmap[k,j])
			{
			    j = MIDIio.size;
			    k = ct;             // easier C# way to break only inner loop?
			    k++;
			}

		    if (j <= CCct[ct] && CCct[ct] < MIDIio.size)
		    {
			CCname[cc] = CCtype[ct] + CCct[ct];
			CCmap[ct, CCct[ct]++] = cc;		// avoid duplication
		    }
		    else MIDIio.Info($"Init():  CCtype[ct] {cc} already assigned to {CCname[cc]}");
		}
	    }
	    // all configured MIDIin properties are now in CCname[] and CCmap[,]

	    // Collect input properties configured for output
            for (ct = 0; ct < SendType.Length; ct++)
            for (j = 0; j < MIDIio.Size[ct]; j++)
            {
                MIDIio.Log(8, $"{SendType[ct]} for {MIDIio.Real[j]}");
                byte k = (2 > j) ? j : (byte)(j + 1);
                for (byte i = 0; i < MIDIio.size; i++)                          // snag Real[j] configured sends
                {

		    string prop = I.PluginManager.GetPropertyValue($"{MIDIio.Ini + SendType[j]}{k}")?.ToString();
		    
		    if (null != prop && 11 < prop.Length)
		    {
			if ("MIDIio." == prop.Substring(0, 7))
			    UniqueCC(prop);
			else if ("Joystic" == prop.Substring(0, 7))
			    UniqueSend(1, prop);
			else if ("InputSt" == prop.Substring(0, 7))
			    UniqueSend(2, prop);
			else UniqueSend(3, prop);
		    }
		    else MIDIio.Info("Init(): unrecognized send property:  " + prop);
                }
            }

	    // Search MIDIio.ini for send properties.  MIDIio.size may have been configured for each, but destinations may accept less, even 0
	    byte cct = 0;							// Configured[] count
	    string[] ss = { "", "", "" };
	    for (ct = 0; ct < SendType.Length; ct++)
	    for (j = 0; j < MIDIio.Real.Length; j++)
	    {
                MIDIio.Log(8, $"{SendType[ct]} for {MIDIio.Real[j]}");
                byte k = (2 > j) ? j : (byte)(j + 1);				// first vJoy button is 1, not 0
		for (byte i = 0; i < MIDIio.size; i++)   			// snag Real[j] configured sends
		{
		    SendProp[j, i] = I.PluginManager.GetPropertyValue($"{MIDIio.Ini + SendType[j]}{k}")?.ToString();
		    if ((null != SendProp[j, i] && 0 < SendProp[j, i].Length))
		    {
			ok[j, i] = true;					// a valid send
			// test for properties with  Real prefix
			if ((3 + MIDIio.Real[j].Length) < SendProp[j, i].Length && MIDIio.Real[j] == (SendProp[j, i].Substring(0, MIDIio.My.Length)))
			{
			    game[j, i] = false;					// Real match, so not game; lots of not ok game remain set to true
			    Map[j][SendCt[ct, j]] = i;
			    if (SendCt[ct, j] < Send[j].Length)
			        Send[j][SendCt[ct, j]] = SendProp[j, i];
			    SendCt[ct, j]++;
			    if (0 == ss[ct].Length)
			        ss[ct] += SendProp[j, i] + ",";
			    else ss[ct] += "\n\t\t\t" + SendProp[j, i] + ",";
			}
		    }
		}
		SendCt[ct, j + 1] = SendCt[ct, j];			// previous SendCt entries are starts for subsequent

		for (byte i = 0; i < MIDIio.size; i++)  	// now, configured sends NOT from MIDIin
		{
		    if (ok[j,i] && game[j, i])				// configured game properties not already assigned to Send[,]
		    {
			if (Map[j].Length > SendCt[ct, j])
			    Map[j][SendCt[ct, j]] = i;
			if (0 < ct && 0 == j && Configured.Length > cct)
			{
			    byte l;

			    for (l = 0; l < cct; l++)
				if (i == Configured[cct])		// duplicate?
				    l = (byte)(cct + 1);
			    if (l <= cct)
			        Configured[cct++] = i;			// CC number to reuse for this send
			}
			if (SendCt[ct, j] < Send[j].Length)
			    Send[j][SendCt[ct, j]] = SendProp[j, i];
			SendCt[ct, j]++;
			if (0 == ss[ct].Length)
			    ss[ct] += SendProp[j, i] + ",";
			else ss[ct] += "\n\t\t\t" + SendProp[j, i] + ",";
		    }
		}
	    }
	    for (ct = 0; ct < SendType.Length; ct++)
	    {
		MIDIio.Log(4, $"Properties.Map.{SendType[ct]} SendCt = {SendCt[ct, SendType.Length]}:  " + Join_bytes(",", Map[ct], SendCt[ct, SendType.Length]));
		MIDIio.Log(4, $"Properties.Send[{ct}]:  " + ss[ct]);
	    }
	}	// Init()

	internal void End(MIDIio I)
	{
	    byte ct; 

	    for (byte i = ct = 0; i < 128; i++)
		if (unconfigured == Which[i])
		{
		    I.Settings.Sent[i] |= 0x80; // flag unconfigured CCs to restore
		    ct++;
		}

	    MIDIio.Log(4, $"Properties.End():  {ct} unconfigured CCs");
	}

	private void Action(MIDIio I, byte bn, byte CCnumber)
	{
	    I.AddEvent(CCname[CCnumber]);
			if (bn < Ping.Length)
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
		    MIDIio.Info($"Action(): invalid Ping[{bn}] for {CCnumber}");
		    break;
	    }
	}

	internal bool SetProp(MIDIio I, int CCnumber, byte value)
	{
	    switch (CCnumber)		// configure CC property and event
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
		    MIDIio.Info($"SetProp() not set: CC{CCnumber}");
		    return false;
	    }
	    CCvalue[CCnumber] = value;
	    return true;
	}	// SetProp()

	private byte mc = 0;					// Configured[] index count
	// Init() identified valid send entries and sorted prop[] names into Send[] with I.my first

	internal void Attach(MIDIio I)	// call SetProp to AttachDelegate() MIDIin properties based on ExternalScript.MIDI* properties
	{
	    byte[] Wmap = new byte[] { unconfigured, CC, CC, Button };		// Which type flag bits
	    string send = MIDIio.Ini + "out";
	    int L = MIDIio.My.Length;
	    byte j, my = 0;

	    if (MIDIio.Log(8, "Attach() MIDIio.in sends:"))
	    {
		for (j = 0; j < Send.GetLength(0); j++)
		    for (byte i = 0; i < SendCt[j, 0]; i++)
			MIDIio.Info("\t" + Send[j][i] + " AKA " + Send[j][i].Substring(L, Send[j][i].Length - L));
	    }

	    for (byte s = 1; s < CCtype.Length; s++)		// reserve s == 0 for sends and unassigned CCs
	    {
		string type = MIDIio.Ini + CCtype[s] + 's';
		string value = I.PluginManager.GetPropertyValue(type)?.ToString();
		if (null == value) {
		    MIDIio.Info($"Attach(): '{type}' not found");
		    continue;
		}

		// bless the Internet
		byte[] array = value.Split(',').Select(byte.Parse).ToArray();
		MIDIio.Log(4, $"Attach(): '{MIDIio.Ini + CCtype[s]}' {string.Join(",", array.Select(p => p.ToString()).ToArray())}");

		byte cn = 0;		 			// index settings[]
		foreach (byte cc in array)			// array has cc numbers assigned for this type
		{
		    bool first = (0 == (Which[cc] & ~unconfigured));
		    if (first)					// CCname[cc] may have been already been assigned for MIDI
		    {
			if(unconfigured == Which[cc])
			{
			    MIDIio.Log(4, $"Attach() replacing {CCname[cc]} by " + CCtype[s] + cn);
			    Which[cc] = 0;
			}
			CCname[cc] = CCtype[s] + cn;	  	// lacks MIDIio.Ini
			Which[cc] |= Wmap[s];

			MIDIio.Log(8, $"Attach():  {CCname[cc]} = CC{cc}");
			for (byte mi = 0; mi < SendCt[0, 0] && mi < Send[0].Length; mi++)
			    if (CCname[cc].Length == (Send[0][mi].Length - L) && CCname[cc] == Send[0][mi].Substring(L, CCname[cc].Length))
			    {
				Unmap[cc] = my;
				Map[0][my++] = cc;			// reuse this input CC number for output
			    }
			    else if (mc < MIDIio.Size[0])
				Configured[mc++] = cc;

 			SetProp(I, cc, I.Settings.Sent[cc]);	// set property for newly configured input
			if (Button == Wmap[s])
			{
			    Which[cc] |= CC;
			    Action(I, cn, cc);
			}
		    }
		    else
		    {
			MIDIio.Log(4, $"{CCtype[s] + cn} previously configured as {CCname[cc]}");
			Which[cc] |= Wmap[s];
		    }
		    cn++;					// next setting[s]
		}
	    }

            // MIDIin property configuration is now complete with mc CC numbers available in Configured[] for MIDIout not occupied by MIDIin properties
	    // there are SendCt[0,0] MIDIin configured to MIDIout
	    // SendCt[0, SendType.Length] - SendCt[0,0] MIDIout configured from other sources than want CC numbers assigned.
            // SendCt[1, 0] + SendCt[2, 0] MIDIin configured to vJoy, some of which may be duplicates
 	    // Not all configured MIDI in are necessarily being used, but configured MIDIin not in Map[0][i] for 0 <= i < SendCt[0,0]  can be remapped.
            // if input CC number count destined for other than MIDIout < configured MIDI outputs from other than MIDIin,
            // then non-MIDIin configured output CC numbers potentially collide with unconfigured MIDIin CC numbers when DoEcho
            if (mc < SendCt[0, 0])
		MIDIio.Info($"Attach(): allocated MIDIin count {mc} < {SendCt[0, 0]} configured for MIDIout");

	    byte ct;
	    if (!MIDIio.DoEcho)
	    {
		for (j = ct = 0; ct < 128; ct++)
		    if (0 < (unconfigured & Which[ct]))
		    {
			SetProp(I, ct, I.Settings.Sent[ct]);	// restore previous received unconfigured CCs
			j++;
		    }
		if (0 < j)
		    MIDIio.Log(4, $"Attach():  {j} previous CC properties restored");
	    }

	    ct = SendCt[0,0];
	    for (my = 0; my < mc && ct < SendCt[0, SendType.Length]; my++) 
		Map[0][ct++] = Configured[my];			// recycle configured MIDIin CC numbers

	    // appropriate previously unallocated CC numbers;  those controls will be unavailable in DoEcho mode..
	    for (my = 127; SendCt[0, MIDIio.Real.Length] <= my && ct < mc && ct < Map[0].Length; my--)	// configure CC numbers for MySendCt <= i < SendCt
	    {
		if (0 == Which[my])
		{
		    Map[0][ct++] = my;				// appropriate unconfigured MIDIin CC numbers
		    Which[my] = unconfigured;			// set my as appropriated
		}
	    }
	    if (ct < mc)
            {
		string ss = SendProp[0, ct++];
		for (; ct < mc; ct++)
                   ss += ", " + SendProp[0, ct];
		MIDIio.Info("Attach(): MIDIout properties not Map[]ed:  " + ss);
            }
	}	// Attach()
    }
}
