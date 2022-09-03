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

        private PluginSettings settings;

        // Alternatively preload them
        private string waterFull = Tools.FileToBase64("res/progress/water_full.png", true);
        private string waterHalf = Tools.FileToBase64("res/progress/water_half.png", true);
        private string waterEmpty = Tools.FileToBase64("res/progress/water_empty.png", true);

        private const int WARNING_BLINK_DELAY = 400;
        private string waterWarning = Tools.FileToBase64("res/progress/water_warning.png", true);
        private string waterWarningAlt = Tools.FileToBase64("res/progress/water_warning_alt.png", true);

        private string countdown = Tools.FileToBase64("res/progress/countdown.png", true);
        private string stats = Tools.FileToBase64("res/progress/stats.png", true);

        // Long press stuff
        private const int LONG_PRESS_DELAY_MS = 600; // Android default is 500
        private bool pressed = false;
        private CancellationTokenSource longPressCancellation;

        // State
        private ActionMode mode = ActionMode.Graphic;
        private DateTime lastDrink = DateTime.Now;

        public WaterReminder(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Constructor called");
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

        #region Overrides

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
                    // Long press happened
                    pressed = false;
                    OnLongPress();

                }
            }
            );
        }

        public override void KeyReleased(KeyPayload payload)
        {
            if (pressed)
            {
                pressed = false;
                OnShortPress();
            }
        }

        public override void OnTick()
        {
            DrawMode(mode);
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #endregion

        private void OnShortPress()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Short press");
            // Cycle the display mode
            switch (mode)
            {
                case ActionMode.Graphic:
                    mode = ActionMode.Countdown;
                    break;
                case ActionMode.Countdown:
                    mode = ActionMode.Stats;
                    break;
                case ActionMode.Stats:
                    mode = ActionMode.Graphic;
                    break;
            }
            DrawMode(mode);
        }

        private void OnLongPress()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Long press");
            // Reset the time
            lastDrink = DateTime.Now;
        }

        private void DrawMode(ActionMode mode)
        {
            switch (mode)
            {
                case ActionMode.Graphic:
                    DrawGraphic();
                    break;
                case ActionMode.Countdown:
                    DrawCountdown();
                    break;
                case ActionMode.Stats:
                    DrawStats();
                    break;
            }
        }

        private void DrawGraphic()
        {
            switch ((DateTime.Now - lastDrink).TotalSeconds)
            {
                case double s when (s <= 8):
                    Connection.SetImageAsync(waterFull);
                    break;
                case double s when (s > 8 && s <= 16):
                    Connection.SetImageAsync(waterHalf);
                    break;
                case double s when (s > 16 && s <= 24):
                    Connection.SetImageAsync(waterEmpty);
                    break;
                default:
                    Connection.SetImageAsync(waterWarning);
                    Task.Delay(WARNING_BLINK_DELAY).ContinueWith(t =>
                    {
                        if ((DateTime.Now - lastDrink).TotalSeconds > 24) // TODO: abstract this conditions
                        {
                            Connection.SetImageAsync(waterWarningAlt);
                        }
                    }
            );
                    break;
            }
            //Logger.Instance.LogMessage(TracingLevel.INFO, "64: " + Tools.FileToBase64("res/action_with_water@2x.png", true));
            //await Connection.SetImageAsync(Image.FromFile("res/action_with_water@2x.png"));
            //await Connection.SetImageAsync(Properties.Plugin.Default.Water100, forceSendToStreamdeck: true);
            //await Connection.SetImageAsync(Tools.FileToBase64("res/action_with_water@2x.png", true));
        }

        private async void DrawCountdown()
        {
            await Connection.SetImageAsync(countdown);
        }

        private async void DrawStats()
        {
            await Connection.SetImageAsync(stats);
        }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
    }
}