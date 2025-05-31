using SimHub.Plugins;

namespace blekenbleu
{
	internal partial class IOproperties
	{
		// Unc 0x80 bit is stripped from I.Settings.CCvalue[]s in Init()
		internal bool CCprop(MIDIio I, int CCnumber)
		{
			switch (CCnumber)		// configure CC property and event
			{
				case 0:
					I.AttachDelegate(CCname[0], () => I.Settings.CCvalue[0]);
					break;
				case 1:
					I.AttachDelegate(CCname[1], () => I.Settings.CCvalue[1]);
					break;
				case 2:
					I.AttachDelegate(CCname[2], () => I.Settings.CCvalue[2]);
					break;
				case 3:
					I.AttachDelegate(CCname[3], () => I.Settings.CCvalue[3]);
					break;
				case 4:
					I.AttachDelegate(CCname[4], () => I.Settings.CCvalue[4]);
					break;
				case 5:
					I.AttachDelegate(CCname[5], () => I.Settings.CCvalue[5]);
					break;
				case 6:
					I.AttachDelegate(CCname[6], () => I.Settings.CCvalue[6]);
					break;
				case 7:
					I.AttachDelegate(CCname[7], () => I.Settings.CCvalue[7]);
					break;
				case 8:
					I.AttachDelegate(CCname[8], () => I.Settings.CCvalue[8]);
					break;
				case 9:
					I.AttachDelegate(CCname[9], () => I.Settings.CCvalue[9]);
					break;
				case 10:
					I.AttachDelegate(CCname[10], () => I.Settings.CCvalue[10]);
					break;
				case 11:
					I.AttachDelegate(CCname[11], () => I.Settings.CCvalue[11]);
					break;
				case 12:
					I.AttachDelegate(CCname[12], () => I.Settings.CCvalue[12]);
					break;
				case 13:
					I.AttachDelegate(CCname[13], () => I.Settings.CCvalue[13]);
					break;
				case 14:
					I.AttachDelegate(CCname[14], () => I.Settings.CCvalue[14]);
					break;
				case 15:
					I.AttachDelegate(CCname[15], () => I.Settings.CCvalue[15]);
					break;
				case 16:
					I.AttachDelegate(CCname[16], () => I.Settings.CCvalue[16]);
					break;
				case 17:
					I.AttachDelegate(CCname[17], () => I.Settings.CCvalue[17]);
					break;
				case 18:
					I.AttachDelegate(CCname[18], () => I.Settings.CCvalue[18]);
					break;
				case 19:
					I.AttachDelegate(CCname[19], () => I.Settings.CCvalue[19]);
					break;
				case 20:
					I.AttachDelegate(CCname[20], () => I.Settings.CCvalue[20]);
					break;
				case 21:
					I.AttachDelegate(CCname[21], () => I.Settings.CCvalue[21]);
					break;
				case 22:
					I.AttachDelegate(CCname[22], () => I.Settings.CCvalue[22]);
					break;
				case 23:
					I.AttachDelegate(CCname[23], () => I.Settings.CCvalue[23]);
					break;
				case 24:
					I.AttachDelegate(CCname[24], () => I.Settings.CCvalue[24]);
					break;
				case 25:
					I.AttachDelegate(CCname[25], () => I.Settings.CCvalue[25]);
					break;
				case 26:
					I.AttachDelegate(CCname[26], () => I.Settings.CCvalue[26]);
					break;
				case 27:
					I.AttachDelegate(CCname[27], () => I.Settings.CCvalue[27]);
					break;
				case 28:
					I.AttachDelegate(CCname[28], () => I.Settings.CCvalue[28]);
					break;
				case 29:
					I.AttachDelegate(CCname[29], () => I.Settings.CCvalue[29]);
					break;
				case 30:
					I.AttachDelegate(CCname[30], () => I.Settings.CCvalue[30]);
					break;
				case 31:
					I.AttachDelegate(CCname[31], () => I.Settings.CCvalue[31]);
					break;
				case 32:
					I.AttachDelegate(CCname[32], () => I.Settings.CCvalue[32]);
					break;
				case 33:
					I.AttachDelegate(CCname[33], () => I.Settings.CCvalue[33]);
					break;
				case 34:
					I.AttachDelegate(CCname[34], () => I.Settings.CCvalue[34]);
					break;
				case 35:
					I.AttachDelegate(CCname[35], () => I.Settings.CCvalue[35]);
					break;
				case 36:
					I.AttachDelegate(CCname[36], () => I.Settings.CCvalue[36]);
					break;
				case 37:
					I.AttachDelegate(CCname[37], () => I.Settings.CCvalue[37]);
					break;
				case 38:
					I.AttachDelegate(CCname[38], () => I.Settings.CCvalue[38]);
					break;
				case 39:
					I.AttachDelegate(CCname[39], () => I.Settings.CCvalue[39]);
					break;
				case 40:
					I.AttachDelegate(CCname[40], () => I.Settings.CCvalue[40]);
					break;
				case 41:
					I.AttachDelegate(CCname[41], () => I.Settings.CCvalue[41]);
					break;
				case 42:
					I.AttachDelegate(CCname[42], () => I.Settings.CCvalue[42]);
					break;
				case 43:
					I.AttachDelegate(CCname[43], () => I.Settings.CCvalue[43]);
					break;
				case 44:
					I.AttachDelegate(CCname[44], () => I.Settings.CCvalue[44]);
					break;
				case 45:
					I.AttachDelegate(CCname[45], () => I.Settings.CCvalue[45]);
					break;
				case 46:
					I.AttachDelegate(CCname[46], () => I.Settings.CCvalue[46]);
					break;
				case 47:
					I.AttachDelegate(CCname[47], () => I.Settings.CCvalue[47]);
					break;
				case 48:
					I.AttachDelegate(CCname[48], () => I.Settings.CCvalue[48]);
					break;
				case 49:
					I.AttachDelegate(CCname[49], () => I.Settings.CCvalue[49]);
					break;
				case 50:
					I.AttachDelegate(CCname[50], () => I.Settings.CCvalue[50]);
					break;
				case 51:
					I.AttachDelegate(CCname[51], () => I.Settings.CCvalue[51]);
					break;
				case 52:
					I.AttachDelegate(CCname[52], () => I.Settings.CCvalue[52]);
					break;
				case 53:
					I.AttachDelegate(CCname[53], () => I.Settings.CCvalue[53]);
					break;
				case 54:
					I.AttachDelegate(CCname[54], () => I.Settings.CCvalue[54]);
					break;
				case 55:
					I.AttachDelegate(CCname[55], () => I.Settings.CCvalue[55]);
					break;
				case 56:
					I.AttachDelegate(CCname[56], () => I.Settings.CCvalue[56]);
					break;
				case 57:
					I.AttachDelegate(CCname[57], () => I.Settings.CCvalue[57]);
					break;
				case 58:
					I.AttachDelegate(CCname[58], () => I.Settings.CCvalue[58]);
					break;
				case 59:
					I.AttachDelegate(CCname[59], () => I.Settings.CCvalue[59]);
					break;
				case 60:
					I.AttachDelegate(CCname[60], () => I.Settings.CCvalue[60]);
					break;
				case 61:
					I.AttachDelegate(CCname[61], () => I.Settings.CCvalue[61]);
					break;
				case 62:
					I.AttachDelegate(CCname[62], () => I.Settings.CCvalue[62]);
					break;
				case 63:
					I.AttachDelegate(CCname[63], () => I.Settings.CCvalue[63]);
					break;
				case 64:
					I.AttachDelegate(CCname[64], () => I.Settings.CCvalue[64]);
					break;
				case 65:
					I.AttachDelegate(CCname[65], () => I.Settings.CCvalue[65]);
					break;
				case 66:
					I.AttachDelegate(CCname[66], () => I.Settings.CCvalue[66]);
					break;
				case 67:
					I.AttachDelegate(CCname[67], () => I.Settings.CCvalue[67]);
					break;
				case 68:
					I.AttachDelegate(CCname[68], () => I.Settings.CCvalue[68]);
					break;
				case 69:
					I.AttachDelegate(CCname[69], () => I.Settings.CCvalue[69]);
					break;
				case 70:
					I.AttachDelegate(CCname[70], () => I.Settings.CCvalue[70]);
					break;
				case 71:
					I.AttachDelegate(CCname[71], () => I.Settings.CCvalue[71]);
					break;
				case 72:
					I.AttachDelegate(CCname[72], () => I.Settings.CCvalue[72]);
					break;
				case 73:
					I.AttachDelegate(CCname[73], () => I.Settings.CCvalue[73]);
					break;
				case 74:
					I.AttachDelegate(CCname[74], () => I.Settings.CCvalue[74]);
					break;
				case 75:
					I.AttachDelegate(CCname[75], () => I.Settings.CCvalue[75]);
					break;
				case 76:
					I.AttachDelegate(CCname[76], () => I.Settings.CCvalue[76]);
					break;
				case 77:
					I.AttachDelegate(CCname[77], () => I.Settings.CCvalue[77]);
					break;
				case 78:
					I.AttachDelegate(CCname[78], () => I.Settings.CCvalue[78]);
					break;
				case 79:
					I.AttachDelegate(CCname[79], () => I.Settings.CCvalue[79]);
					break;
				case 80:
					I.AttachDelegate(CCname[80], () => I.Settings.CCvalue[80]);
					break;
				case 81:
					I.AttachDelegate(CCname[81], () => I.Settings.CCvalue[81]);
					break;
				case 82:
					I.AttachDelegate(CCname[82], () => I.Settings.CCvalue[82]);
					break;
				case 83:
					I.AttachDelegate(CCname[83], () => I.Settings.CCvalue[83]);
					break;
				case 84:
					I.AttachDelegate(CCname[84], () => I.Settings.CCvalue[84]);
					break;
				case 85:
					I.AttachDelegate(CCname[85], () => I.Settings.CCvalue[85]);
					break;
				case 86:
					I.AttachDelegate(CCname[86], () => I.Settings.CCvalue[86]);
					break;
				case 87:
					I.AttachDelegate(CCname[87], () => I.Settings.CCvalue[87]);
					break;
				case 88:
					I.AttachDelegate(CCname[88], () => I.Settings.CCvalue[88]);
					break;
				case 89:
					I.AttachDelegate(CCname[89], () => I.Settings.CCvalue[89]);
					break;
				case 90:
					I.AttachDelegate(CCname[90], () => I.Settings.CCvalue[90]);
					break;
				case 91:
					I.AttachDelegate(CCname[91], () => I.Settings.CCvalue[91]);
					break;
				case 92:
					I.AttachDelegate(CCname[92], () => I.Settings.CCvalue[92]);
					break;
				case 93:
					I.AttachDelegate(CCname[93], () => I.Settings.CCvalue[93]);
					break;
				case 94:
					I.AttachDelegate(CCname[94], () => I.Settings.CCvalue[94]);
					break;
				case 95:
					I.AttachDelegate(CCname[95], () => I.Settings.CCvalue[95]);
					break;
				case 96:
					I.AttachDelegate(CCname[96], () => I.Settings.CCvalue[96]);
					break;
				case 97:
					I.AttachDelegate(CCname[97], () => I.Settings.CCvalue[97]);
					break;
				case 98:
					I.AttachDelegate(CCname[98], () => I.Settings.CCvalue[98]);
					break;
				case 99:
					I.AttachDelegate(CCname[99], () => I.Settings.CCvalue[99]);
					break;
				case 100:
					I.AttachDelegate(CCname[100], () => I.Settings.CCvalue[100]);
					break;
				case 101:
					I.AttachDelegate(CCname[101], () => I.Settings.CCvalue[101]);
					break;
				case 102:
					I.AttachDelegate(CCname[102], () => I.Settings.CCvalue[102]);
					break;
				case 103:
					I.AttachDelegate(CCname[103], () => I.Settings.CCvalue[103]);
					break;
				case 104:
					I.AttachDelegate(CCname[104], () => I.Settings.CCvalue[104]);
					break;
				case 105:
					I.AttachDelegate(CCname[105], () => I.Settings.CCvalue[105]);
					break;
				case 106:
					I.AttachDelegate(CCname[106], () => I.Settings.CCvalue[106]);
					break;
				case 107:
					I.AttachDelegate(CCname[107], () => I.Settings.CCvalue[107]);
					break;
				case 108:
					I.AttachDelegate(CCname[108], () => I.Settings.CCvalue[108]);
					break;
				case 109:
					I.AttachDelegate(CCname[109], () => I.Settings.CCvalue[109]);
					break;
				case 110:
					I.AttachDelegate(CCname[110], () => I.Settings.CCvalue[110]);
					break;
				case 111:
					I.AttachDelegate(CCname[111], () => I.Settings.CCvalue[111]);
					break;
				case 112:
					I.AttachDelegate(CCname[112], () => I.Settings.CCvalue[112]);
					break;
				case 113:
					I.AttachDelegate(CCname[113], () => I.Settings.CCvalue[113]);
					break;
				case 114:
					I.AttachDelegate(CCname[114], () => I.Settings.CCvalue[114]);
					break;
				case 115:
					I.AttachDelegate(CCname[115], () => I.Settings.CCvalue[115]);
					break;
				case 116:
					I.AttachDelegate(CCname[116], () => I.Settings.CCvalue[116]);
					break;
				case 117:
					I.AttachDelegate(CCname[117], () => I.Settings.CCvalue[117]);
					break;
				case 118:
					I.AttachDelegate(CCname[118], () => I.Settings.CCvalue[118]);
					break;
				case 119:
					I.AttachDelegate(CCname[119], () => I.Settings.CCvalue[119]);
					break;
				case 120:
					I.AttachDelegate(CCname[120], () => I.Settings.CCvalue[120]);
					break;
				case 121:
					I.AttachDelegate(CCname[121], () => I.Settings.CCvalue[121]);
					break;
				case 122:
					I.AttachDelegate(CCname[122], () => I.Settings.CCvalue[122]);
					break;
				case 123:
					I.AttachDelegate(CCname[123], () => I.Settings.CCvalue[123]);
					break;
				case 124:
					I.AttachDelegate(CCname[124], () => I.Settings.CCvalue[124]);
					break;
				case 125:
					I.AttachDelegate(CCname[125], () => I.Settings.CCvalue[125]);
					break;
				case 126:
					I.AttachDelegate(CCname[126], () => I.Settings.CCvalue[126]);
					break;
				case 127:
					I.AttachDelegate(CCname[127], () => I.Settings.CCvalue[127]);
					break;
				default:
					MIDIio.Info($"CCprop() not set: CC{CCnumber}");
					return false;
			}
			I.Settings.CCvalue[CCnumber] = I.Settings.CCvalue[CCnumber];		// SimHub otherwise shows 0
			return true;
		}	// CCprop()
	}
}
