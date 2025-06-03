using SimHub.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace blekenbleu
{
	internal partial class IOproperties
	{
		private  List<string> Send;					  		// send Actions

		internal void EnumActions(MIDIio I, PluginManager pluginManager, string[] actions)
		{
			for (byte a = 0; a < actions.Length; a++)
				if (2 > actions[a].Length)
					MIDIio.Log(0, $"IOproperties.EnumActions({actions[a]}): invalid MIDIsends value");
				else
				{
					string s = MIDIio.Ini + "send" + actions[a];
					string prop = (string)pluginManager.GetPropertyValue(s);

					if (null == prop || 8 > prop.Length)
						MIDIio.Log(0, $"IOproperties.Action({s}):  dubious property name :" + prop);
					if (byte.TryParse(actions[a].Substring(1), out byte addr))
						SendAdd(I, actions[a][0], addr, prop);
					else MIDIio.Log(0, $"IOproperties.Action({actions[a]}): invalid byte address");
				}
			MIDIio.Log(4, "Leaving EnumActions");
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

		// call CCprop to AttachDelegate() configured MIDIin properties
		internal void Attach(MIDIio I)
		{
			byte j, cc, st;

			if (MIDIio.Log(4, ""))
			{
				string s = "Attach() non-MIDI source properties:\n";
				List<string> nonMIDI = new List<string>();

				// search thru all non-MIDI source
				for (st = 0; st < 3; st++)
					for (j = 0; j < SourceList[st].Count; j++)
						if(NoDup(SourceList[st][j].Name, ref nonMIDI))
							s += "\t" + SourceList[st][j].Name + "\n";
				MIDIio.Info(s);
			}

			for (cc = 0; cc < 128; cc++)
				if (0 < (3 & Which[cc]))
 					CCprop(I, cc, false);				// set property for configured input

			// MIDIin property configuration is now complete

			if (MIDIio.DoEcho)
			{
				for (j = cc = 0; cc < 128; cc++)
					if (0 < (Unc & Which[cc]))
					{
						CCprop(I, cc, true);			// restore previous received unconfigured CCs
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
