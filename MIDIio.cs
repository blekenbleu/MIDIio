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
        private byte size = 8;						// Maximum Ping, etc to configure
        internal byte[] Size;
        private bool[,] Once;

        internal string Ini = "DataCorePlugin.ExternalScript.MIDI";	// configuration source
        internal string My = "MIDIio.";					// PluginName + '.'
        internal MIDIioSettings Settings;
        internal CCProperties Properties;
        internal VJsend VJD;
        internal INdrywet Reader;
        internal OUTdrywet Outer;
        private byte Level = 0;
        internal bool DoEcho = false;

        internal bool Info(string str)
        {
            SimHub.Logging.Current.Info(str);
            return true;
        }

        internal bool Log(byte level, string str)
        {
            bool b = 0 < (level & Level);

            if (b)
                SimHub.Logging.Current.Info(str);
            return b;
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
                DoSend(pluginManager, false);

            // Send my property messages anytime (echo)
            DoSend(pluginManager, true);
//          VJD.Run();
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            Reader.End();
            Outer.End();
            VJD.End();
            Properties.End(this);
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        /// <summary>
        /// Called at SimHub start then after game changes
        /// </summary>
        /// <param name="pluginManager"></param>
        private static int count = 0;
        public void Init(PluginManager pluginManager)
        {
            Log(4, My + "Init()");
            // Load settings
            Settings = this.ReadCommonSettings<MIDIioSettings>("GeneralSettings", () => new MIDIioSettings());

            VJD = new VJsend();			// vJoy
            VJD.Init(this, 1);			// obtain joystick button and axis counts VJD.nButtons, VJD.nAxes

            Properties = new CCProperties();    // MIDI and vJoy property configuration
            Properties.Init(this);		// set SendCt[] before Outer.Init()

            string output = pluginManager.GetPropertyValue(Ini + "log")?.ToString();
            // Log() level configuration
            if (null != output && 0 < output.Length)
                Level = (byte)(0.5 + Convert.ToDouble(output));

            // Launch Outer before Reader, which tries to send stored MIDI CC messages
            output = pluginManager.GetPropertyValue(Ini + "out")?.ToString();
            if (null == output || 0 == output.Length) {
                Info(My + ".out: unassigned");
                pluginManager.AddProperty("out", this.GetType(), "unassigned");
            }
            else pluginManager.AddProperty("out", this.GetType(), output);

            DoEcho = 0 < Int32.Parse(pluginManager.GetPropertyValue(Ini + "echo")?.ToString());
            Info(My + "Init(): unconfigured CCs will" + (DoEcho ? "" : " not") + " be echo'ed"); 

            Outer = new OUTdrywet();
            Outer.Init(this, output, Properties.SendCt[0]);

            string input = pluginManager.GetPropertyValue(Ini + "in")?.ToString();
            if (0 < input.Length)
            {
                pluginManager.AddProperty("in", this.GetType(), input);
                Reader = new INdrywet();
                Reader.Init(input, this);
            }
            else Info(My + "Init(): " + Ini + "in is invalid: '" + input +"'" );

            count += 1;		// increments for each restart, provoked e.g. by game change or restart
            pluginManager.AddProperty(My + "Init().count", this.GetType(), count);
            Once = new bool[Properties.send.Length, size];
            Size = new byte[] {size, (size < VJD.nAxes) ? size : VJD.nAxes, (size < VJD.nButtons) ? size : VJD.nButtons};

            for (int i = 0; i < size; i++)
                Settings.Sent[Properties.Map[0, i]] = 129;	// impossible first Send[i] values
            for (byte j = 0; j < Properties.send.Length; j++)
                for (int i = 0; i < Size[j]; i++}
                    Once[j, i] = true;

//          data = pluginManager.GetPropertyValue("DataCorePlugin.CustomExpression.MIDIsliders");
//          pluginManager.AddProperty("sliders", this.GetType(), (null == data) ? "unassigned" : data.ToString());
        }

        // track active CCs and save values
        internal bool Active(byte CCnumber, byte value)			// returns true if first time
        {
            byte which = Properties.Which[CCnumber];
            if (5 == CCnumber)
                Log(8, Properties.CCname[CCnumber]);			// just a debugging breakpoint

            if (0 < which)	// Configured?
            {
                if (0 < which && Properties.CCvalue[CCnumber] != value)
                {
                    Outer.Latest = Properties.CCvalue[CCnumber] = value;// drop pass to Ping()
                    if (0 < (Properties.Button & which))
                        this.TriggerEvent(Properties.CCname[CCnumber]);
                    if (DoEcho && 0 < (Properties.unconfigured & which))
                        return !Outer.SendCCval(CCnumber, value); 	// unconfigured may also be appropriated configured
                }
                return false;
            }

            if (DoEcho)
            {
                if (value != Properties.CCvalue[CCnumber])				// send only changes
                    Outer.SendCCval(CCnumber, Properties.CCvalue[CCnumber] = value);	// do not SetProp()
                return true;
            }

            // First time CC number seen
            Properties.Which[CCnumber] = Properties.unconfigured;	// dynamic CC configuration
//          this.AddEvent(Properties.CCname[CCnumber]);			// Users may assign CCn events to e.g. Ping()
//          this.TriggerEvent(Properties.CCname[CCnumber]);
            return Properties.SetProp(this, CCnumber, Properties.CCvalue[CCnumber] = value);
        }	// Active()

        private void DoSend(PluginManager pluginManager, bool always)
        {
            for (byte j = 0; j < Properties.send.Length; j++)
            for (byte b = (byte)((always) ? 0 : Properties.MySendCt[j]) ; b < ((always) ? Properties.MySendCt[j] : Properties.SendCt[j]); b++)
            {
                byte cc = Properties.Map[j, b];	// MIDIout CC numbers

                if (!Once[j, b])
                   return;

                string prop = Properties.Send[j, b];
                string send = pluginManager.GetPropertyValue(prop)?.ToString();

                if (null == send)
                {
                     Once[j, b] = false;
                     Info(My + "DataUpdate(): null " + prop);
                }
                else if (0 < send.Length)
                {
                    double property = Convert.ToDouble(send);
                    if (Properties.MySendCt[j] <= b)
                        property *= 1.27;				// ShakeIt properties <= 100
                    byte value = (byte)(0.5 * property);

                    value &= 0x7F;
                    if (Settings.Sent[cc] != value) {			// send only changed values
                        Settings.Sent[cc] = value;
                        if (Properties.VJax == (Properties.VJax & Properties.Which[b]))
                            VJD.Axis(Properties.Map[1, b], property);
                        if (Properties.VJbut == (Properties.VJbut & Properties.Which[b]))
                            VJD.Button(Properties.Map[2, b], 0.5 < property);
                        if (Properties.CC == (Properties.CC & Properties.Which[b]))
			    Outer.SendCCval(cc, value);	// DoSendCC()
	            }
                }
            }
        }		// DoSend()
    }
}
