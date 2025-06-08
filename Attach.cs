using SimHub.Plugins;
using System.Collections.Generic;

namespace blekenbleu
{
	internal partial class IOproperties
	{
		// called after all non-MIDIsends configured inputs and outputs
		internal void EnumActions(PluginManager pluginManager, string[] actions)
		{
			for (byte a = 0; a < actions.Length; a++)
				if (2 > actions[a].Length)
					MIDIio.Log(0, MIDIio.oops = $"IOproperties.EnumActions({actions[a]}): invalid MIDIsends value");
				else
				{
					string s = MIDIio.Ini + "send" + actions[a];
					string prop = (string)pluginManager.GetPropertyValue(s);

					if (null == prop || 8 > prop.Length)
						MIDIio.Log(0, MIDIio.oops = $"IOproperties.Action({s}):  dubious property name :" + prop);
					else if (byte.TryParse(actions[a].Substring(1), out byte addr))
						M.SendAdd(actions[a][0], addr, prop);
					else MIDIio.Log(0, $"IOproperties.Action({actions[a]}): invalid byte address");
				}
        }

		bool NoDup(string prop, ref List<string> plist)
		{
			if (null == prop)
				return false;

			for (int i = plist.Count - 1; i >= 0; i--)
				if (plist[i] == prop)
					return false;

			plist.Add(prop);
			return true;
		}

		internal void Attach()
		{
			byte cc, st;
			short j;

			if (MIDIio.Log(4, ""))
			{
				string s = "";
				List<string> nonMIDI = new List<string>();

				// search thru all non-MIDI source
				for (st = 0; st < 3; st++)
					for (j = (short)(SourceList[st].Count - 1); j >= 0; j--)
					{
						string SE;

						if (j >= M.stop[st])
							SE = " (SendEvent)";
						else SE = "";
						if (NoDup(SourceList[st][j].Name, ref nonMIDI))
							s += "\t" + SourceList[st][j].Name + SE + "\n";
					}
				if (0 < s.Length)
					MIDIio.Info("Attach() non-MIDI source properties:\n" + s);
			}

			for (cc = 0; cc < 128; cc++)
				if (0 < (CC & Which[cc]))
 					CCprop(cc, false);				// AttachDelegate() configured input property

			// MIDIin property configuration is now complete

			if (MIDIio.DoEcho)
			{
				for (j = cc = 0; cc < 128; cc++)
					if (0 < (Unc & Which[cc]))
					{
						CCprop(cc, true);			// restore previous unconfigured CCs;  AttachDelegate()
						j++;
					}

				if (0 < j)
					MIDIio.Log(4, $"Attach():  {j} previous CC properties restored");
			}
		}	// Attach()

		internal void End(MIDIio I)
		{
			byte ct; 

			for (byte i = ct = 0; MIDIio.DoEcho && i < 128; i++)
				if (Unc == Which[i])
				{
					I.Settings.CCvalue[i] |= 0x80; // flag unconfigured CCs to restore
					ct++;
				}
			if (0 < ct)
				MIDIio.Log(4, $"IOProperties.End():  {ct} unconfigured CCs");
		}
	}
}
