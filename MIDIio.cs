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
            for ( ; b < to; b++)
            {
                byte cc = CCProperties.Remap[b];	// MIDIout CC numbers

                if (!Once[cc])
                   return;

                string prop = CCProperties.Send[cc];
                string send = pluginManager.GetPropertyValue(prop)?.ToString();

                if (null == send )
                {
                     Once[cc] = false;
                     SimHub.Logging.Current.Info("MIDIio DataUpdate(): null " + prop);
                }
                else
                {
                    byte value = (byte)Convert.ToDouble(send);

                    if (Settings.Sent[cc] != value)
                        Outer.SendCCval(cc, Settings.Sent[cc] = value);
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
            string output = pluginManager.GetPropertyValue(Ini + "out")?.ToString();
            if (null == output || 0 == output.Length) {
                output = "unassigned";
                SimHub.Logging.Current.Info("MIDIio.out: " + output);
            }
            pluginManager.AddProperty("out", this.GetType(), output);
            Outer = new OUTdrywet();
            Outer.Init(this, output, CCProperties.SendCt);

            output = pluginManager.GetPropertyValue(Ini + "echo")?.ToString();
            int val = Int32.Parse(output);
//          SimHub.Logging.Current.Info($"MIDIio {Ini}echo = {output} AKA {val}");
            if (!(DoEcho = (0 < val)))
               SimHub.Logging.Current.Info("MIDIio(): unconfigured CCs will not be echo'ed"); 

            string input = pluginManager.GetPropertyValue(Ini + "in")?.ToString();
            if (0 == input.Length)
                input = "unassigned";
            else {
                pluginManager.AddProperty("in", this.GetType(), input);
                Reader = new INdrywet();
                Reader.Init(input, Settings, this);
            }
            SimHub.Logging.Current.Info("MIDIio.in: " + input);

            count += 1;		// increments for each Init(), provoked e.g. by game change or restart
            pluginManager.AddProperty("Init() count", this.GetType(), count);

//          data = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.MIDIsliders");
//          pluginManager.AddProperty("sliders", this.GetType(), (null == data) ? "unassigned" : data.ToString());
        }

        // track active CCs and save values
        internal bool Active(byte CCnumber, byte value)	// returns true if first time
        {
            byte which = CCProperties.Which[CCnumber];

            if (0 < which)	// Configured?
            {
                CCProperties.CCvalue[CCnumber] = value;
                if (3 == which && 0 < value)	// button?
                    this.TriggerEvent(CCProperties.CCname[CCnumber]);
                return false;
            }

            if (DoEcho)
                return !Outer.SendCCval(CCnumber, value);	// do not log

            ulong mask = 1;
            byte index = 0;

            if (63 < CCnumber)					// convert from 0-127 to 0-63, index
                index++;                			// switch ulong
            mask <<= (63 & CCnumber);

            if (mask == (mask & Settings.CCbits[index]))      	// already set?
                return (255 < (CCProperties.CCvalue[CCnumber] = value));

            // First time CC number seen
            Settings.CCbits[index] |= mask;
            return CCProperties.SetProp(this, CCnumber, value);
        }
    }
}
