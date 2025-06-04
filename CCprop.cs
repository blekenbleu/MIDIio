using SimHub.Plugins;

namespace blekenbleu
{
	internal partial class IOproperties
	{
		// Unc 0x80 bit is stripped from M.Settings.CCvalue[]s in Init()
		internal bool CCprop(int CCnumber, bool setUnc)
		{
//			MIDIio.Log(4, $"CCprop({CCname[CCnumber]})");
			if ( 0 < (SendEvent & Which[CCnumber]))
			{
				Which[CCnumber] |= CC;
				Which[CCnumber] &= (byte)~Unc;
			}
			else if (setUnc)
				Which[CCnumber] = Unc;

			switch (CCnumber)		// configure CC property and event
			{
				case 0:
					M.AttachDelegate(CCname[0], () => M.Settings.CCvalue[0]);
					break;
				case 1:
					M.AttachDelegate(CCname[1], () => M.Settings.CCvalue[1]);
					break;
				case 2:
					M.AttachDelegate(CCname[2], () => M.Settings.CCvalue[2]);
					break;
				case 3:
					M.AttachDelegate(CCname[3], () => M.Settings.CCvalue[3]);
					break;
				case 4:
					M.AttachDelegate(CCname[4], () => M.Settings.CCvalue[4]);
					break;
				case 5:
					M.AttachDelegate(CCname[5], () => M.Settings.CCvalue[5]);
					break;
				case 6:
					M.AttachDelegate(CCname[6], () => M.Settings.CCvalue[6]);
					break;
				case 7:
					M.AttachDelegate(CCname[7], () => M.Settings.CCvalue[7]);
					break;
				case 8:
					M.AttachDelegate(CCname[8], () => M.Settings.CCvalue[8]);
					break;
				case 9:
					M.AttachDelegate(CCname[9], () => M.Settings.CCvalue[9]);
					break;
				case 10:
					M.AttachDelegate(CCname[10], () => M.Settings.CCvalue[10]);
					break;
				case 11:
					M.AttachDelegate(CCname[11], () => M.Settings.CCvalue[11]);
					break;
				case 12:
					M.AttachDelegate(CCname[12], () => M.Settings.CCvalue[12]);
					break;
				case 13:
					M.AttachDelegate(CCname[13], () => M.Settings.CCvalue[13]);
					break;
				case 14:
					M.AttachDelegate(CCname[14], () => M.Settings.CCvalue[14]);
					break;
				case 15:
					M.AttachDelegate(CCname[15], () => M.Settings.CCvalue[15]);
					break;
				case 16:
					M.AttachDelegate(CCname[16], () => M.Settings.CCvalue[16]);
					break;
				case 17:
					M.AttachDelegate(CCname[17], () => M.Settings.CCvalue[17]);
					break;
				case 18:
					M.AttachDelegate(CCname[18], () => M.Settings.CCvalue[18]);
					break;
				case 19:
					M.AttachDelegate(CCname[19], () => M.Settings.CCvalue[19]);
					break;
				case 20:
					M.AttachDelegate(CCname[20], () => M.Settings.CCvalue[20]);
					break;
				case 21:
					M.AttachDelegate(CCname[21], () => M.Settings.CCvalue[21]);
					break;
				case 22:
					M.AttachDelegate(CCname[22], () => M.Settings.CCvalue[22]);
					break;
				case 23:
					M.AttachDelegate(CCname[23], () => M.Settings.CCvalue[23]);
					break;
				case 24:
					M.AttachDelegate(CCname[24], () => M.Settings.CCvalue[24]);
					break;
				case 25:
					M.AttachDelegate(CCname[25], () => M.Settings.CCvalue[25]);
					break;
				case 26:
					M.AttachDelegate(CCname[26], () => M.Settings.CCvalue[26]);
					break;
				case 27:
					M.AttachDelegate(CCname[27], () => M.Settings.CCvalue[27]);
					break;
				case 28:
					M.AttachDelegate(CCname[28], () => M.Settings.CCvalue[28]);
					break;
				case 29:
					M.AttachDelegate(CCname[29], () => M.Settings.CCvalue[29]);
					break;
				case 30:
					M.AttachDelegate(CCname[30], () => M.Settings.CCvalue[30]);
					break;
				case 31:
					M.AttachDelegate(CCname[31], () => M.Settings.CCvalue[31]);
					break;
				case 32:
					M.AttachDelegate(CCname[32], () => M.Settings.CCvalue[32]);
					break;
				case 33:
					M.AttachDelegate(CCname[33], () => M.Settings.CCvalue[33]);
					break;
				case 34:
					M.AttachDelegate(CCname[34], () => M.Settings.CCvalue[34]);
					break;
				case 35:
					M.AttachDelegate(CCname[35], () => M.Settings.CCvalue[35]);
					break;
				case 36:
					M.AttachDelegate(CCname[36], () => M.Settings.CCvalue[36]);
					break;
				case 37:
					M.AttachDelegate(CCname[37], () => M.Settings.CCvalue[37]);
					break;
				case 38:
					M.AttachDelegate(CCname[38], () => M.Settings.CCvalue[38]);
					break;
				case 39:
					M.AttachDelegate(CCname[39], () => M.Settings.CCvalue[39]);
					break;
				case 40:
					M.AttachDelegate(CCname[40], () => M.Settings.CCvalue[40]);
					break;
				case 41:
					M.AttachDelegate(CCname[41], () => M.Settings.CCvalue[41]);
					break;
				case 42:
					M.AttachDelegate(CCname[42], () => M.Settings.CCvalue[42]);
					break;
				case 43:
					M.AttachDelegate(CCname[43], () => M.Settings.CCvalue[43]);
					break;
				case 44:
					M.AttachDelegate(CCname[44], () => M.Settings.CCvalue[44]);
					break;
				case 45:
					M.AttachDelegate(CCname[45], () => M.Settings.CCvalue[45]);
					break;
				case 46:
					M.AttachDelegate(CCname[46], () => M.Settings.CCvalue[46]);
					break;
				case 47:
					M.AttachDelegate(CCname[47], () => M.Settings.CCvalue[47]);
					break;
				case 48:
					M.AttachDelegate(CCname[48], () => M.Settings.CCvalue[48]);
					break;
				case 49:
					M.AttachDelegate(CCname[49], () => M.Settings.CCvalue[49]);
					break;
				case 50:
					M.AttachDelegate(CCname[50], () => M.Settings.CCvalue[50]);
					break;
				case 51:
					M.AttachDelegate(CCname[51], () => M.Settings.CCvalue[51]);
					break;
				case 52:
					M.AttachDelegate(CCname[52], () => M.Settings.CCvalue[52]);
					break;
				case 53:
					M.AttachDelegate(CCname[53], () => M.Settings.CCvalue[53]);
					break;
				case 54:
					M.AttachDelegate(CCname[54], () => M.Settings.CCvalue[54]);
					break;
				case 55:
					M.AttachDelegate(CCname[55], () => M.Settings.CCvalue[55]);
					break;
				case 56:
					M.AttachDelegate(CCname[56], () => M.Settings.CCvalue[56]);
					break;
				case 57:
					M.AttachDelegate(CCname[57], () => M.Settings.CCvalue[57]);
					break;
				case 58:
					M.AttachDelegate(CCname[58], () => M.Settings.CCvalue[58]);
					break;
				case 59:
					M.AttachDelegate(CCname[59], () => M.Settings.CCvalue[59]);
					break;
				case 60:
					M.AttachDelegate(CCname[60], () => M.Settings.CCvalue[60]);
					break;
				case 61:
					M.AttachDelegate(CCname[61], () => M.Settings.CCvalue[61]);
					break;
				case 62:
					M.AttachDelegate(CCname[62], () => M.Settings.CCvalue[62]);
					break;
				case 63:
					M.AttachDelegate(CCname[63], () => M.Settings.CCvalue[63]);
					break;
				case 64:
					M.AttachDelegate(CCname[64], () => M.Settings.CCvalue[64]);
					break;
				case 65:
					M.AttachDelegate(CCname[65], () => M.Settings.CCvalue[65]);
					break;
				case 66:
					M.AttachDelegate(CCname[66], () => M.Settings.CCvalue[66]);
					break;
				case 67:
					M.AttachDelegate(CCname[67], () => M.Settings.CCvalue[67]);
					break;
				case 68:
					M.AttachDelegate(CCname[68], () => M.Settings.CCvalue[68]);
					break;
				case 69:
					M.AttachDelegate(CCname[69], () => M.Settings.CCvalue[69]);
					break;
				case 70:
					M.AttachDelegate(CCname[70], () => M.Settings.CCvalue[70]);
					break;
				case 71:
					M.AttachDelegate(CCname[71], () => M.Settings.CCvalue[71]);
					break;
				case 72:
					M.AttachDelegate(CCname[72], () => M.Settings.CCvalue[72]);
					break;
				case 73:
					M.AttachDelegate(CCname[73], () => M.Settings.CCvalue[73]);
					break;
				case 74:
					M.AttachDelegate(CCname[74], () => M.Settings.CCvalue[74]);
					break;
				case 75:
					M.AttachDelegate(CCname[75], () => M.Settings.CCvalue[75]);
					break;
				case 76:
					M.AttachDelegate(CCname[76], () => M.Settings.CCvalue[76]);
					break;
				case 77:
					M.AttachDelegate(CCname[77], () => M.Settings.CCvalue[77]);
					break;
				case 78:
					M.AttachDelegate(CCname[78], () => M.Settings.CCvalue[78]);
					break;
				case 79:
					M.AttachDelegate(CCname[79], () => M.Settings.CCvalue[79]);
					break;
				case 80:
					M.AttachDelegate(CCname[80], () => M.Settings.CCvalue[80]);
					break;
				case 81:
					M.AttachDelegate(CCname[81], () => M.Settings.CCvalue[81]);
					break;
				case 82:
					M.AttachDelegate(CCname[82], () => M.Settings.CCvalue[82]);
					break;
				case 83:
					M.AttachDelegate(CCname[83], () => M.Settings.CCvalue[83]);
					break;
				case 84:
					M.AttachDelegate(CCname[84], () => M.Settings.CCvalue[84]);
					break;
				case 85:
					M.AttachDelegate(CCname[85], () => M.Settings.CCvalue[85]);
					break;
				case 86:
					M.AttachDelegate(CCname[86], () => M.Settings.CCvalue[86]);
					break;
				case 87:
					M.AttachDelegate(CCname[87], () => M.Settings.CCvalue[87]);
					break;
				case 88:
					M.AttachDelegate(CCname[88], () => M.Settings.CCvalue[88]);
					break;
				case 89:
					M.AttachDelegate(CCname[89], () => M.Settings.CCvalue[89]);
					break;
				case 90:
					M.AttachDelegate(CCname[90], () => M.Settings.CCvalue[90]);
					break;
				case 91:
					M.AttachDelegate(CCname[91], () => M.Settings.CCvalue[91]);
					break;
				case 92:
					M.AttachDelegate(CCname[92], () => M.Settings.CCvalue[92]);
					break;
				case 93:
					M.AttachDelegate(CCname[93], () => M.Settings.CCvalue[93]);
					break;
				case 94:
					M.AttachDelegate(CCname[94], () => M.Settings.CCvalue[94]);
					break;
				case 95:
					M.AttachDelegate(CCname[95], () => M.Settings.CCvalue[95]);
					break;
				case 96:
					M.AttachDelegate(CCname[96], () => M.Settings.CCvalue[96]);
					break;
				case 97:
					M.AttachDelegate(CCname[97], () => M.Settings.CCvalue[97]);
					break;
				case 98:
					M.AttachDelegate(CCname[98], () => M.Settings.CCvalue[98]);
					break;
				case 99:
					M.AttachDelegate(CCname[99], () => M.Settings.CCvalue[99]);
					break;
				case 100:
					M.AttachDelegate(CCname[100], () => M.Settings.CCvalue[100]);
					break;
				case 101:
					M.AttachDelegate(CCname[101], () => M.Settings.CCvalue[101]);
					break;
				case 102:
					M.AttachDelegate(CCname[102], () => M.Settings.CCvalue[102]);
					break;
				case 103:
					M.AttachDelegate(CCname[103], () => M.Settings.CCvalue[103]);
					break;
				case 104:
					M.AttachDelegate(CCname[104], () => M.Settings.CCvalue[104]);
					break;
				case 105:
					M.AttachDelegate(CCname[105], () => M.Settings.CCvalue[105]);
					break;
				case 106:
					M.AttachDelegate(CCname[106], () => M.Settings.CCvalue[106]);
					break;
				case 107:
					M.AttachDelegate(CCname[107], () => M.Settings.CCvalue[107]);
					break;
				case 108:
					M.AttachDelegate(CCname[108], () => M.Settings.CCvalue[108]);
					break;
				case 109:
					M.AttachDelegate(CCname[109], () => M.Settings.CCvalue[109]);
					break;
				case 110:
					M.AttachDelegate(CCname[110], () => M.Settings.CCvalue[110]);
					break;
				case 111:
					M.AttachDelegate(CCname[111], () => M.Settings.CCvalue[111]);
					break;
				case 112:
					M.AttachDelegate(CCname[112], () => M.Settings.CCvalue[112]);
					break;
				case 113:
					M.AttachDelegate(CCname[113], () => M.Settings.CCvalue[113]);
					break;
				case 114:
					M.AttachDelegate(CCname[114], () => M.Settings.CCvalue[114]);
					break;
				case 115:
					M.AttachDelegate(CCname[115], () => M.Settings.CCvalue[115]);
					break;
				case 116:
					M.AttachDelegate(CCname[116], () => M.Settings.CCvalue[116]);
					break;
				case 117:
					M.AttachDelegate(CCname[117], () => M.Settings.CCvalue[117]);
					break;
				case 118:
					M.AttachDelegate(CCname[118], () => M.Settings.CCvalue[118]);
					break;
				case 119:
					M.AttachDelegate(CCname[119], () => M.Settings.CCvalue[119]);
					break;
				case 120:
					M.AttachDelegate(CCname[120], () => M.Settings.CCvalue[120]);
					break;
				case 121:
					M.AttachDelegate(CCname[121], () => M.Settings.CCvalue[121]);
					break;
				case 122:
					M.AttachDelegate(CCname[122], () => M.Settings.CCvalue[122]);
					break;
				case 123:
					M.AttachDelegate(CCname[123], () => M.Settings.CCvalue[123]);
					break;
				case 124:
					M.AttachDelegate(CCname[124], () => M.Settings.CCvalue[124]);
					break;
				case 125:
					M.AttachDelegate(CCname[125], () => M.Settings.CCvalue[125]);
					break;
				case 126:
					M.AttachDelegate(CCname[126], () => M.Settings.CCvalue[126]);
					break;
				case 127:
					M.AttachDelegate(CCname[127], () => M.Settings.CCvalue[127]);
					break;
				default:
					MIDIio.Info($"CCprop() not set: CC{CCnumber}");
					return false;
			}
			M.Settings.CCvalue[CCnumber] = M.Settings.CCvalue[CCnumber];		// SimHub otherwise shows 0
			return true;
		}	// CCprop()
	}
}
