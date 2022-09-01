using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace com.jorkoh.health.reminders
{
    [PluginActionId("com.jorkoh.health.reminders.water")]
    public class WaterReminder : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    OutputFileName = String.Empty,
                    InputString = String.Empty
                };
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "outputFileName")]
            public string OutputFileName { get; set; }

            [JsonProperty(PropertyName = "inputString")]
            public string InputString { get; set; }
        }

        #region Private Members

        private PluginSettings settings;

        // Alternatively preload them
        private string waterFull = Tools.FileToBase64("res/progress/water_full.png", true);
        private string waterHalf = Tools.FileToBase64("res/progress/water_half.png", true);
        private string waterEmpty = Tools.FileToBase64("res/progress/water_empty.png", true);
        private DateTime lastDrink;

        // Long press stuff
        private const int LONG_PRESS_DELAY_MS = 600; // Android default is 500
        private bool pressed = false;
        private CancellationTokenSource longPressCancellation;

        #endregion
        public WaterReminder(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                SaveSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");

            // Cancel the previous long press task first
            longPressCancellation?.Cancel();
            pressed = true;
            longPressCancellation = new CancellationTokenSource();
            Task.Delay(LONG_PRESS_DELAY_MS, longPressCancellation.Token).ContinueWith(t =>
            {
                if (pressed && !t.IsCanceled)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, "Long press");
                    Connection.ShowOk();
                }
            }
            );
        }

        public override void KeyReleased(KeyPayload payload) {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Released");
            pressed = false;
        }

        public async override void OnTick()
        {
            if (lastDrink == null)
            {
                // First tick init
                lastDrink = DateTime.Now;
                return;
            }

            switch ((DateTime.Now - lastDrink).TotalSeconds)
            {
                case double s when (s <= 6):
                    await Connection.SetImageAsync(waterFull);
                    break;
                case double s when (s > 6 && s <= 12):
                    await Connection.SetImageAsync(waterHalf);
                    break;
                case double s when (s > 12 && s <= 18):
                    await Connection.SetImageAsync(waterEmpty);
                    break;
                default:
                    lastDrink = DateTime.Now;
                    break;
            }
            //Logger.Instance.LogMessage(TracingLevel.INFO, "64: " + Tools.FileToBase64("res/action_with_water@2x.png", true));
            //await Connection.SetImageAsync(Image.FromFile("res/action_with_water@2x.png"));
            //await Connection.SetImageAsync(Properties.Plugin.Default.Water100, forceSendToStreamdeck: true);
            //await Connection.SetImageAsync(Tools.FileToBase64("res/action_with_water@2x.png", true));
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        #endregion
    }
}