using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
                List<CycleLength> lengths = new List<CycleLength>()
                {
                    new CycleLength()
                    {
                        CycleLengthName = "5 minutes",
                        CycleLengthSeconds = 5*60
                    },
                    new CycleLength()
                    {
                        CycleLengthName = "10 minutes",
                        CycleLengthSeconds = 10*60
                    },
                    new CycleLength()
                    {
                        CycleLengthName = "15 minutes",
                        CycleLengthSeconds = 15*60
                    },
                    new CycleLength()
                    {
                        CycleLengthName = "30 minutes",
                        CycleLengthSeconds = 30*60
                    },
                    new CycleLength()
                    {
                        CycleLengthName = "1 hour",
                        CycleLengthSeconds = 60*60
                    },
                    new CycleLength()
                    {
                        CycleLengthName = "2 hours",
                        CycleLengthSeconds = 2*60*60
                    }
                };

                PluginSettings instance = new PluginSettings
                {
                    CycleLengths = lengths,
                    CycleLengthSeconds = lengths[0].CycleLengthSeconds
                };
                return instance;
            }

            [JsonProperty(PropertyName = "cycleLengths")]
            public List<CycleLength> CycleLengths { get; set; }

            [JsonProperty(PropertyName = "cycleLengthSeconds")]
            public int CycleLengthSeconds { get; set; }
        }

        private PluginSettings settings;

        // Alternatively preload them
        private string water10 = Tools.FileToBase64("res/progress/water_10.png", true);
        private string water9 = Tools.FileToBase64("res/progress/water_9.png", true);
        private string water8 = Tools.FileToBase64("res/progress/water_8.png", true);
        private string water7 = Tools.FileToBase64("res/progress/water_7.png", true);
        private string water6 = Tools.FileToBase64("res/progress/water_6.png", true);
        private string water5 = Tools.FileToBase64("res/progress/water_5.png", true);
        private string water4 = Tools.FileToBase64("res/progress/water_4.png", true);
        private string water3 = Tools.FileToBase64("res/progress/water_3.png", true);
        private string water2 = Tools.FileToBase64("res/progress/water_2.png", true);
        private string water1 = Tools.FileToBase64("res/progress/water_1.png", true);
        private string waterWarning = Tools.FileToBase64("res/progress/water_0.png", true);
        private string waterWarningAlt = Tools.FileToBase64("res/progress/water_0_alt.png", true);

        private string countdown = Tools.FileToBase64("res/progress/countdown_temp.png", true);
        private string stats = Tools.FileToBase64("res/progress/stats_temp.png", true);
        private bool altWarning = false; // Blinking alert

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
                Connection.SetSettingsAsync(JObject.FromObject(settings));
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
            Connection.SetSettingsAsync(JObject.FromObject(settings));
            Logger.Instance.LogMessage(TracingLevel.INFO, $"SETTING: {settings.CycleLengthSeconds}");
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
            altWarning = false;
            Connection.ShowOk();
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
            switch ((DateTime.Now - lastDrink).TotalSeconds / settings.CycleLengthSeconds)
            {
                case double percentage when (percentage < 0.1):
                    Connection.SetImageAsync(water10);
                    break;
                case double s when (s < 0.2):
                    Connection.SetImageAsync(water9);
                    break;
                case double s when (s < 0.3):
                    Connection.SetImageAsync(water8);
                    break;
                case double s when (s < 0.4):
                    Connection.SetImageAsync(water7);
                    break;
                case double s when (s < 0.5):
                    Connection.SetImageAsync(water6);
                    break;
                case double s when (s < 0.6):
                    Connection.SetImageAsync(water5);
                    break;
                case double s when (s < 0.7):
                    Connection.SetImageAsync(water4);
                    break;
                case double s when (s < 0.8):
                    Connection.SetImageAsync(water3);
                    break;
                case double s when (s < 0.9):
                    Connection.SetImageAsync(water2);
                    break;
                case double s when (s < 1):
                    Connection.SetImageAsync(water1);
                    break;
                default:
                    if (!altWarning)
                    {
                        Connection.SetImageAsync(waterWarning);
                    }
                    else
                    {
                        Connection.SetImageAsync(waterWarningAlt);
                    }
                    altWarning = !altWarning;
                    break;
            }
        }

        private async void DrawCountdown()
        {
            await Connection.SetImageAsync(countdown);
        }

        private async void DrawStats()
        {
            await Connection.SetImageAsync(stats);
        }
    }
}