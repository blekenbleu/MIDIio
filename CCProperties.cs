﻿using System;
using SimHub.Plugins;

namespace blekenbleu.MIDIspace
{
    // Dynamically add events and properties for CC buttons as pressed
    // To Do: save and restore 
    internal class CCProperties
    {
        internal readonly string[] CCname = new string[128]  // INdrywet() uses in TriggerEvent()
        {
            "CC0",
            "CC1",
            "CC2",
            "CC3",
            "CC4",
            "CC5",
            "CC6",
            "CC7",
            "CC8",
            "CC9",
            "CC10",
            "CC11",
            "CC12",
            "CC13",
            "CC14",
            "CC15",
            "CC16",
            "CC17",
            "CC18",
            "CC19",
            "CC20",
            "CC21",
            "CC22",
            "CC23",
            "CC24",
            "CC25",
            "CC26",
            "CC27",
            "CC28",
            "CC29",
            "CC30",
            "CC31",
            "CC32",
            "CC33",
            "CC34",
            "CC35",
            "CC36",
            "CC37",
            "CC38",
            "CC39",
            "CC40",
            "CC41",
            "CC42",
            "CC43",
            "CC44",
            "CC45",
            "CC46",
            "CC47",
            "CC48",
            "CC49",
            "CC50",
            "CC51",
            "CC52",
            "CC53",
            "CC54",
            "CC55",
            "CC56",
            "CC57",
            "CC58",
            "CC59",
            "CC60",
            "CC61",
            "CC62",
            "CC63",
            "CC64",
            "CC65",
            "CC66",
            "CC67",
            "CC68",
            "CC69",
            "CC70",
            "CC71",
            "CC72",
            "CC73",
            "CC74",
            "CC75",
            "CC76",
            "CC77",
            "CC78",
            "CC79",
            "CC80",
            "CC81",
            "CC82",
            "CC83",
            "CC84",
            "CC85",
            "CC86",
            "CC87",
            "CC88",
            "CC89",
            "CC90",
            "CC91",
            "CC92",
            "CC93",
            "CC94",
            "CC95",
            "CC96",
            "CC97",
            "CC98",
            "CC99",
            "CC100",
            "CC101",
            "CC102",
            "CC103",
            "CC104",
            "CC105",
            "CC106",
            "CC107",
            "CC108",
            "CC109",
            "CC110",
            "CC111",
            "CC112",
            "CC113",
            "CC114",
            "CC115",
            "CC116",
            "CC117",
            "CC118",
            "CC119",
            "CC120",
            "CC121",
            "CC122",
            "CC123",
            "CC124",
            "CC125",
            "CC126",
            "CC127"
        };
        private readonly static string[] Action = new string[8]
        {
            "ping0",
            "ping1",
            "ping2",
            "ping3",
            "ping4",
            "ping5",
            "ping6",
            "ping7"
        };

       	private Byte[] CCvalue { get; set; } = new byte[128];   // store CCvalues
        internal void Init()
        {
            for (int i = 0; i < 127; i++)
                CCvalue[i] = 0;
        } 

       	internal bool SetVal(byte CCnumber, byte value)		// INdrywet uses
       	{
       	    CCvalue[CCnumber] = value;
      	    return true;
       	}

        // Declare actions which can be called
        internal void SetPing(MIDIio I, byte CCnumber)		// OUTdrywet uses
        {
            I.AddAction(Action[CCnumber],(a, b) => I.Outer.Ping((Melanchall.DryWetMidi.Common.SevenBitNumber)CCnumber));
        }

        internal void SetProp(MIDIio I, byte CCnumber, byte value)	// INdrywet uses
        {
            CCvalue[CCnumber] = value;
            switch (CCnumber)       // Initialize CC property and event
            {
                case 0:
                    I.AttachDelegate(CCname[0], () => CCvalue[0]);
                    I.AddEvent(CCname[0]);
                    break;
                case 1:
                    I.AttachDelegate(CCname[1], () => CCvalue[1]);
                    I.AddEvent(CCname[1]);
                    break;
                case 2:
                    I.AttachDelegate(CCname[2], () => CCvalue[2]);
                    I.AddEvent(CCname[2]);
                    break;
                case 3:
                    I.AttachDelegate(CCname[3], () => CCvalue[3]);
                    I.AddEvent(CCname[3]);
                    break;
                case 4:
                    I.AttachDelegate(CCname[4], () => CCvalue[4]);
                    I.AddEvent(CCname[4]);
                    break;
                case 5:
                    I.AttachDelegate(CCname[5], () => CCvalue[5]);
                    I.AddEvent(CCname[5]);
                    break;
                case 6:
                    I.AttachDelegate(CCname[6], () => CCvalue[6]);
                    I.AddEvent(CCname[6]);
                    break;
                case 7:
                    I.AttachDelegate(CCname[7], () => CCvalue[7]);
                    I.AddEvent(CCname[7]);
                    break;
                case 8:
                    I.AttachDelegate(CCname[8], () => CCvalue[8]);
                    I.AddEvent(CCname[8]);
                    break;
                case 9:
                    I.AttachDelegate(CCname[9], () => CCvalue[9]);
                    I.AddEvent(CCname[9]);
                    break;
                case 10:
                    I.AttachDelegate(CCname[10], () => CCvalue[10]);
                    I.AddEvent(CCname[10]);
                    break;
                case 11:
                    I.AttachDelegate(CCname[11], () => CCvalue[11]);
                    I.AddEvent(CCname[11]);
                    break;
                case 12:
                    I.AttachDelegate(CCname[12], () => CCvalue[12]);
                    I.AddEvent(CCname[12]);
                    break;
                case 13:
                    I.AttachDelegate(CCname[13], () => CCvalue[13]);
                    I.AddEvent(CCname[13]);
                    break;
                case 14:
                    I.AttachDelegate(CCname[14], () => CCvalue[14]);
                    I.AddEvent(CCname[14]);
                    break;
                case 15:
                    I.AttachDelegate(CCname[15], () => CCvalue[15]);
                    I.AddEvent(CCname[15]);
                    break;
                case 16:
                    I.AttachDelegate(CCname[16], () => CCvalue[16]);
                    I.AddEvent(CCname[16]);
                    break;
                case 17:
                    I.AttachDelegate(CCname[17], () => CCvalue[17]);
                    I.AddEvent(CCname[17]);
                    break;
                case 18:
                    I.AttachDelegate(CCname[18], () => CCvalue[18]);
                    I.AddEvent(CCname[18]);
                    break;
                case 19:
                    I.AttachDelegate(CCname[19], () => CCvalue[19]);
                    I.AddEvent(CCname[19]);
                    break;
                case 20:
                    I.AttachDelegate(CCname[20], () => CCvalue[20]);
                    I.AddEvent(CCname[20]);
                    break;
                case 21:
                    I.AttachDelegate(CCname[21], () => CCvalue[21]);
                    I.AddEvent(CCname[21]);
                    break;
                case 22:
                    I.AttachDelegate(CCname[22], () => CCvalue[22]);
                    I.AddEvent(CCname[22]);
                    break;
                case 23:
                    I.AttachDelegate(CCname[23], () => CCvalue[23]);
                    I.AddEvent(CCname[23]);
                    break;
                case 24:
                    I.AttachDelegate(CCname[24], () => CCvalue[24]);
                    I.AddEvent(CCname[24]);
                    break;
                case 25:
                    I.AttachDelegate(CCname[25], () => CCvalue[25]);
                    I.AddEvent(CCname[25]);
                    break;
                case 26:
                    I.AttachDelegate(CCname[26], () => CCvalue[26]);
                    I.AddEvent(CCname[26]);
                    break;
                case 27:
                    I.AttachDelegate(CCname[27], () => CCvalue[27]);
                    I.AddEvent(CCname[27]);
                    break;
                case 28:
                    I.AttachDelegate(CCname[28], () => CCvalue[28]);
                    I.AddEvent(CCname[28]);
                    break;
                case 29:
                    I.AttachDelegate(CCname[29], () => CCvalue[29]);
                    I.AddEvent(CCname[29]);
                    break;
                case 30:
                    I.AttachDelegate(CCname[30], () => CCvalue[30]);
                    I.AddEvent(CCname[30]);
                    break;
                case 31:
                    I.AttachDelegate(CCname[31], () => CCvalue[31]);
                    I.AddEvent(CCname[31]);
                    break;
                case 32:
                    I.AttachDelegate(CCname[32], () => CCvalue[32]);
                    I.AddEvent(CCname[32]);
                    break;
                case 33:
                    I.AttachDelegate(CCname[33], () => CCvalue[33]);
                    I.AddEvent(CCname[33]);
                    break;
                case 34:
                    I.AttachDelegate(CCname[34], () => CCvalue[34]);
                    I.AddEvent(CCname[34]);
                    break;
                case 35:
                    I.AttachDelegate(CCname[35], () => CCvalue[35]);
                    I.AddEvent(CCname[35]);
                    break;
                case 36:
                    I.AttachDelegate(CCname[36], () => CCvalue[36]);
                    I.AddEvent(CCname[36]);
                    break;
                case 37:
                    I.AttachDelegate(CCname[37], () => CCvalue[37]);
                    I.AddEvent(CCname[37]);
                    break;
                case 38:
                    I.AttachDelegate(CCname[38], () => CCvalue[38]);
                    I.AddEvent(CCname[38]);
                    break;
                case 39:
                    I.AttachDelegate(CCname[39], () => CCvalue[39]);
                    I.AddEvent(CCname[39]);
                    break;
                case 40:
                    I.AttachDelegate(CCname[40], () => CCvalue[40]);
                    I.AddEvent(CCname[40]);
                    break;
                case 41:
                    I.AttachDelegate(CCname[41], () => CCvalue[41]);
                    I.AddEvent(CCname[41]);
                    break;
                case 42:
                    I.AttachDelegate(CCname[42], () => CCvalue[42]);
                    I.AddEvent(CCname[42]);
                    break;
                case 43:
                    I.AttachDelegate(CCname[43], () => CCvalue[43]);
                    I.AddEvent(CCname[43]);
                    break;
                case 44:
                    I.AttachDelegate(CCname[44], () => CCvalue[44]);
                    I.AddEvent(CCname[44]);
                    break;
                case 45:
                    I.AttachDelegate(CCname[45], () => CCvalue[45]);
                    I.AddEvent(CCname[45]);
                    break;
                case 46:
                    I.AttachDelegate(CCname[46], () => CCvalue[46]);
                    I.AddEvent(CCname[46]);
                    break;
                case 47:
                    I.AttachDelegate(CCname[47], () => CCvalue[47]);
                    I.AddEvent(CCname[47]);
                    break;
                case 48:
                    I.AttachDelegate(CCname[48], () => CCvalue[48]);
                    I.AddEvent(CCname[48]);
                    break;
                case 49:
                    I.AttachDelegate(CCname[49], () => CCvalue[49]);
                    I.AddEvent(CCname[49]);
                    break;
                case 50:
                    I.AttachDelegate(CCname[50], () => CCvalue[50]);
                    I.AddEvent(CCname[50]);
                    break;
                case 51:
                    I.AttachDelegate(CCname[51], () => CCvalue[51]);
                    I.AddEvent(CCname[51]);
                    break;
                case 52:
                    I.AttachDelegate(CCname[52], () => CCvalue[52]);
                    I.AddEvent(CCname[52]);
                    break;
                case 53:
                    I.AttachDelegate(CCname[53], () => CCvalue[53]);
                    I.AddEvent(CCname[53]);
                    break;
                case 54:
                    I.AttachDelegate(CCname[54], () => CCvalue[54]);
                    I.AddEvent(CCname[54]);
                    break;
                case 55:
                    I.AttachDelegate(CCname[55], () => CCvalue[55]);
                    I.AddEvent(CCname[55]);
                    break;
                case 56:
                    I.AttachDelegate(CCname[56], () => CCvalue[56]);
                    I.AddEvent(CCname[56]);
                    break;
                case 57:
                    I.AttachDelegate(CCname[57], () => CCvalue[57]);
                    I.AddEvent(CCname[57]);
                    break;
                case 58:
                    I.AttachDelegate(CCname[58], () => CCvalue[58]);
                    I.AddEvent(CCname[58]);
                    break;
                case 59:
                    I.AttachDelegate(CCname[59], () => CCvalue[59]);
                    I.AddEvent(CCname[59]);
                    break;
                case 60:
                    I.AttachDelegate(CCname[60], () => CCvalue[60]);
                    I.AddEvent(CCname[60]);
                    break;
                case 61:
                    I.AttachDelegate(CCname[61], () => CCvalue[61]);
                    I.AddEvent(CCname[61]);
                    break;
                case 62:
                    I.AttachDelegate(CCname[62], () => CCvalue[62]);
                    I.AddEvent(CCname[62]);
                    break;
                case 63:
                    I.AttachDelegate(CCname[63], () => CCvalue[63]);
                    I.AddEvent(CCname[63]);
                    break;
                case 64:
                    I.AttachDelegate(CCname[64], () => CCvalue[64]);
                    I.AddEvent(CCname[64]);
                    break;
                case 65:
                    I.AttachDelegate(CCname[65], () => CCvalue[65]);
                    I.AddEvent(CCname[65]);
                    break;
                case 66:
                    I.AttachDelegate(CCname[66], () => CCvalue[66]);
                    I.AddEvent(CCname[66]);
                    break;
                case 67:
                    I.AttachDelegate(CCname[67], () => CCvalue[67]);
                    I.AddEvent(CCname[67]);
                    break;
                case 68:
                    I.AttachDelegate(CCname[68], () => CCvalue[68]);
                    I.AddEvent(CCname[68]);
                    break;
                case 69:
                    I.AttachDelegate(CCname[69], () => CCvalue[69]);
                    I.AddEvent(CCname[69]);
                    break;
                case 70:
                    I.AttachDelegate(CCname[70], () => CCvalue[70]);
                    I.AddEvent(CCname[70]);
                    break;
                case 71:
                    I.AttachDelegate(CCname[71], () => CCvalue[71]);
                    I.AddEvent(CCname[71]);
                    break;
                case 72:
                    I.AttachDelegate(CCname[72], () => CCvalue[72]);
                    I.AddEvent(CCname[72]);
                    break;
                case 73:
                    I.AttachDelegate(CCname[73], () => CCvalue[73]);
                    I.AddEvent(CCname[73]);
                    break;
                case 74:
                    I.AttachDelegate(CCname[74], () => CCvalue[74]);
                    I.AddEvent(CCname[74]);
                    break;
                case 75:
                    I.AttachDelegate(CCname[75], () => CCvalue[75]);
                    I.AddEvent(CCname[75]);
                    break;
                case 76:
                    I.AttachDelegate(CCname[76], () => CCvalue[76]);
                    I.AddEvent(CCname[76]);
                    break;
                case 77:
                    I.AttachDelegate(CCname[77], () => CCvalue[77]);
                    I.AddEvent(CCname[77]);
                    break;
                case 78:
                    I.AttachDelegate(CCname[78], () => CCvalue[78]);
                    I.AddEvent(CCname[78]);
                    break;
                case 79:
                    I.AttachDelegate(CCname[79], () => CCvalue[79]);
                    I.AddEvent(CCname[79]);
                    break;
                case 80:
                    I.AttachDelegate(CCname[80], () => CCvalue[80]);
                    I.AddEvent(CCname[80]);
                    break;
                case 81:
                    I.AttachDelegate(CCname[81], () => CCvalue[81]);
                    I.AddEvent(CCname[81]);
                    break;
                case 82:
                    I.AttachDelegate(CCname[82], () => CCvalue[82]);
                    I.AddEvent(CCname[82]);
                    break;
                case 83:
                    I.AttachDelegate(CCname[83], () => CCvalue[83]);
                    I.AddEvent(CCname[83]);
                    break;
                case 84:
                    I.AttachDelegate(CCname[84], () => CCvalue[84]);
                    I.AddEvent(CCname[84]);
                    break;
                case 85:
                    I.AttachDelegate(CCname[85], () => CCvalue[85]);
                    I.AddEvent(CCname[85]);
                    break;
                case 86:
                    I.AttachDelegate(CCname[86], () => CCvalue[86]);
                    I.AddEvent(CCname[86]);
                    break;
                case 87:
                    I.AttachDelegate(CCname[87], () => CCvalue[87]);
                    I.AddEvent(CCname[87]);
                    break;
                case 88:
                    I.AttachDelegate(CCname[88], () => CCvalue[88]);
                    I.AddEvent(CCname[88]);
                    break;
                case 89:
                    I.AttachDelegate(CCname[89], () => CCvalue[89]);
                    I.AddEvent(CCname[89]);
                    break;
                case 90:
                    I.AttachDelegate(CCname[90], () => CCvalue[90]);
                    I.AddEvent(CCname[90]);
                    break;
                case 91:
                    I.AttachDelegate(CCname[91], () => CCvalue[91]);
                    I.AddEvent(CCname[91]);
                    break;
                case 92:
                    I.AttachDelegate(CCname[92], () => CCvalue[92]);
                    I.AddEvent(CCname[92]);
                    break;
                case 93:
                    I.AttachDelegate(CCname[93], () => CCvalue[93]);
                    I.AddEvent(CCname[93]);
                    break;
                case 94:
                    I.AttachDelegate(CCname[94], () => CCvalue[94]);
                    I.AddEvent(CCname[94]);
                    break;
                case 95:
                    I.AttachDelegate(CCname[95], () => CCvalue[95]);
                    I.AddEvent(CCname[95]);
                    break;
                case 96:
                    I.AttachDelegate(CCname[96], () => CCvalue[96]);
                    I.AddEvent(CCname[96]);
                    break;
                case 97:
                    I.AttachDelegate(CCname[97], () => CCvalue[97]);
                    I.AddEvent(CCname[97]);
                    break;
                case 98:
                    I.AttachDelegate(CCname[98], () => CCvalue[98]);
                    I.AddEvent(CCname[98]);
                    break;
                case 99:
                    I.AttachDelegate(CCname[99], () => CCvalue[99]);
                    I.AddEvent(CCname[99]);
                    break;
                case 100:
                    I.AttachDelegate(CCname[100], () => CCvalue[100]);
                    I.AddEvent(CCname[100]);
                    break;
                case 101:
                    I.AttachDelegate(CCname[101], () => CCvalue[101]);
                    I.AddEvent(CCname[101]);
                    break;
                case 102:
                    I.AttachDelegate(CCname[102], () => CCvalue[102]);
                    I.AddEvent(CCname[102]);
                    break;
                case 103:
                    I.AttachDelegate(CCname[103], () => CCvalue[103]);
                    I.AddEvent(CCname[103]);
                    break;
                case 104:
                    I.AttachDelegate(CCname[104], () => CCvalue[104]);
                    I.AddEvent(CCname[104]);
                    break;
                case 105:
                    I.AttachDelegate(CCname[105], () => CCvalue[105]);
                    I.AddEvent(CCname[105]);
                    break;
                case 106:
                    I.AttachDelegate(CCname[106], () => CCvalue[106]);
                    I.AddEvent(CCname[106]);
                    break;
                case 107:
                    I.AttachDelegate(CCname[107], () => CCvalue[107]);
                    I.AddEvent(CCname[107]);
                    break;
                case 108:
                    I.AttachDelegate(CCname[108], () => CCvalue[108]);
                    I.AddEvent(CCname[108]);
                    break;
                case 109:
                    I.AttachDelegate(CCname[109], () => CCvalue[109]);
                    I.AddEvent(CCname[109]);
                    break;
                case 110:
                    I.AttachDelegate(CCname[110], () => CCvalue[110]);
                    I.AddEvent(CCname[110]);
                    break;
                case 111:
                    I.AttachDelegate(CCname[111], () => CCvalue[111]);
                    I.AddEvent(CCname[111]);
                    break;
                case 112:
                    I.AttachDelegate(CCname[112], () => CCvalue[112]);
                    I.AddEvent(CCname[112]);
                    break;
                case 113:
                    I.AttachDelegate(CCname[113], () => CCvalue[113]);
                    I.AddEvent(CCname[113]);
                    break;
                case 114:
                    I.AttachDelegate(CCname[114], () => CCvalue[114]);
                    I.AddEvent(CCname[114]);
                    break;
                case 115:
                    I.AttachDelegate(CCname[115], () => CCvalue[115]);
                    I.AddEvent(CCname[115]);
                    break;
                case 116:
                    I.AttachDelegate(CCname[116], () => CCvalue[116]);
                    I.AddEvent(CCname[116]);
                    break;
                case 117:
                    I.AttachDelegate(CCname[117], () => CCvalue[117]);
                    I.AddEvent(CCname[117]);
                    break;
                case 118:
                    I.AttachDelegate(CCname[118], () => CCvalue[118]);
                    I.AddEvent(CCname[118]);
                    break;
                case 119:
                    I.AttachDelegate(CCname[119], () => CCvalue[119]);
                    I.AddEvent(CCname[119]);
                    break;
                case 120:
                    I.AttachDelegate(CCname[120], () => CCvalue[120]);
                    I.AddEvent(CCname[120]);
                    break;
                case 121:
                    I.AttachDelegate(CCname[121], () => CCvalue[121]);
                    I.AddEvent(CCname[121]);
                    break;
                case 122:
                    I.AttachDelegate(CCname[122], () => CCvalue[122]);
                    I.AddEvent(CCname[122]);
                    break;
                case 123:
                    I.AttachDelegate(CCname[123], () => CCvalue[123]);
                    I.AddEvent(CCname[123]);
                    break;
                case 124:
                    I.AttachDelegate(CCname[124], () => CCvalue[124]);
                    I.AddEvent(CCname[124]);
                    break;
                case 125:
                    I.AttachDelegate(CCname[125], () => CCvalue[125]);
                    I.AddEvent(CCname[125]);
                    break;
                case 126:
                    I.AttachDelegate(CCname[126], () => CCvalue[126]);
                    I.AddEvent(CCname[126]);
                    break;
                case 127:
                    I.AttachDelegate(CCname[127], () => CCvalue[127]);
                    I.AddEvent(CCname[127]);
                    break;
                default:
                    SimHub.Logging.Current.Info($"SetProp() not set: CC{CCnumber}");
                    break;
            }
            if (0 < value)
                I.TriggerEvent(CCname[CCnumber]);
        }
    }
}
