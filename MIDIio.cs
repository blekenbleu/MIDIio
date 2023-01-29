using GameReaderCommon;
using SimHub.Plugins;
using System;

namespace blekenbleu.MIDIspace
{
    [PluginDescription("MIDI slider I/O")]
    [PluginAuthor("blekenbleu")]
    [PluginName("MIDIio")]
    public class MIDIio : IPlugin, IDataPlugin
    {
        internal string Ini = "DataCorePlugin.ExternalScript.MIDI";	// configuration source
        internal MIDIioSettings Settings;
        internal CCProperties CCProperties;
        internal bool DoEcho = false;

        internal INdrywet Reader;
        internal OUTdrywet Outer;
        private bool[] Once { get; set; } = { true, true, true, true, true, true, true, true };

        private void DoSend(PluginManager pluginManager, byte b, byte to)
        {
            // Lowest MIDIout CC numbers are reserved for MIDIsend0 to (SendCt-1)
            for ( ; b < to; b++)
            {
                string prop = CCProperties.Send[b];
 //             object get = pluginManager.GetPropertyValue(prop);
                String send = pluginManager.GetPropertyValue(prop)?.ToString();

                if (null != send)
                {
                    byte value = (byte)Convert.ToDouble(send);

                    if (Settings.Sent[b] != value)
                        Outer.SendCCval(b, Settings.Sent[b] = value);
                }
                else if (Once[b])
                {
                     Once[b] = false;
                     SimHub.Logging.Current.Info("MIDIio DataUpdate(): null " + prop);
                }
            }
        }

        /// <summary>
        /// Instance of the current plugin manager
        /// </summary>
        public PluginManager PluginManager { get; set; }

        /// <summary>
        /// Called one time per game data update, contains all normalized game data,
        /// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        ///
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        ///
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data">Current game data, including current and previous data frame.</param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            if (data.GameRunning && data.OldData != null && data.NewData != null)
                DoSend(pluginManager, CCProperties.MySendCt, CCProperties.SendCt);

            // Send MIDIio property messages anytime (echo)
            DoSend(pluginManager, 0, CCProperties.MySendCt);
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            // Save settings returned by Device.End()
            Outer.End();
            this.SaveCommonSettings("GeneralSettings", Reader.End());
        }

        /// <summary>
        /// Called at SimHub start then after game changes
        /// </summary>
        /// <param name="pluginManager"></param>
        private static int count = 0;
        public void Init(PluginManager pluginManager)
        {
            // Load settings
            Settings = this.ReadCommonSettings<MIDIioSettings>("GeneralSettings", () => new MIDIioSettings());

            // Make properties available
            // these get evaluated "on demand" (when shown or used in formulas)
            CCProperties = new CCProperties();
            CCProperties.Init(this);		// set SendCt before Outer

            // Launch Outer before Reader, which tries to send stored MIDI CC messages
            object data = pluginManager.GetPropertyValue(Ini + "out");
            String output = data?.ToString();
            pluginManager.AddProperty("out", this.GetType(), (null == data) ? "unassigned" : output);
            SimHub.Logging.Current.Info("MIDIio output device: " + output);
            Outer = new OUTdrywet();
            Outer.Init(this, output, CCProperties.SendCt);

            data = pluginManager.GetPropertyValue(Ini + "echo");
            output = data?.ToString();
            int val = Int32.Parse(output);
//          SimHub.Logging.Current.Info($"MIDIio {Ini}echo = {output} AKA {val}");
            if (!(DoEcho = (0 < val)))
               SimHub.Logging.Current.Info("MIDIio: unconfigured CCs will not be echo'ed"); 

            data = pluginManager.GetPropertyValue(Ini + "in");
            String input = (null == data) ? "unassigned" : data.ToString();
            pluginManager.AddProperty("in", this.GetType(), input);
            SimHub.Logging.Current.Info("MIDIio input device: " + input);
            Reader = new INdrywet();
            Reader.Init(input, Settings, this);

            count += 1;		// increments for each Init(), provoked e.g. by game change or restart
            pluginManager.AddProperty("Init() count", this.GetType(), count);

//          data = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.MIDIsliders");
//          pluginManager.AddProperty("sliders", this.GetType(), (null == data) ? "unassigned" : data.ToString());
        }

        // track active CCs and save values
        internal bool Active(byte CCnumber, byte value)
        {
            ulong mask = 1;
            byte index = 0;
            byte remapped = CCProperties.Remap[CCnumber];

            switch (CCProperties.Which[CCnumber])	// Which[] is not remapped
            {
                case 1:
                    Settings.Slider[remapped] = value;
                    return false;
                case 2:
                    Settings.Knob[remapped] = value;
                    return false;
                case 3:
                    Settings.Button[remapped] = value;
                    if (0 < value)
                        this.TriggerEvent(CCProperties.CCname[remapped]);
                    return false;
            }

            if (DoEcho)
                return !Outer.SendCCval(remapped, value);	// do not log

            if (63 < CCnumber)
                index++;                			// switch ulong
            mask <<= (63 & CCnumber);

            if (mask == (mask & Settings.CCbits[index]))      	// already set?
            {
                CCProperties.CCvalue[CCnumber] = value;
                return false;                           	// do not log
            }

                                                        	// First time CC number seen
            Settings.CCbits[index] |= mask;
            CCProperties.SetProp(this, CCnumber, value);
            return true;
        }


    }
}
