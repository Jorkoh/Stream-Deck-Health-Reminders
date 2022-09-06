using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
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
                        CycleLengthName = "30 seconds", // TODO remove before release
                        CycleLengthSeconds = 30
                    },
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
                        CycleLengthName = "1 hour, 30 minutes",
                        CycleLengthSeconds = 90*60
                    },
                    new CycleLength()
                    {
                        CycleLengthName = "2 hours",
                        CycleLengthSeconds = 120*60
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
        private TitleParameters titleParameters = null;

        // Alternatively preload them
        private readonly string water10 = Tools.FileToBase64("res/progress/water_10.png", true);
        private readonly string water9 = Tools.FileToBase64("res/progress/water_9.png", true);
        private readonly string water8 = Tools.FileToBase64("res/progress/water_8.png", true);
        private readonly string water7 = Tools.FileToBase64("res/progress/water_7.png", true);
        private readonly string water6 = Tools.FileToBase64("res/progress/water_6.png", true);
        private readonly string water5 = Tools.FileToBase64("res/progress/water_5.png", true);
        private readonly string water4 = Tools.FileToBase64("res/progress/water_4.png", true);
        private readonly string water3 = Tools.FileToBase64("res/progress/water_3.png", true);
        private readonly string water2 = Tools.FileToBase64("res/progress/water_2.png", true);
        private readonly string water1 = Tools.FileToBase64("res/progress/water_1.png", true);
        private readonly string waterWarning = Tools.FileToBase64("res/progress/water_0.png", true);
        private readonly string waterWarningAlt = Tools.FileToBase64("res/progress/water_0_alt.png", true);

        private readonly string empty10_path = "res/progress/empty_10.png";
        private readonly string empty9_path = "res/progress/empty_9.png";
        private readonly string empty8_path = "res/progress/empty_8.png";
        private readonly string empty7_path = "res/progress/empty_7.png";
        private readonly string empty6_path = "res/progress/empty_6.png";
        private readonly string empty5_path = "res/progress/empty_5.png";
        private readonly string empty4_path = "res/progress/empty_4.png";
        private readonly string empty3_path = "res/progress/empty_3.png";
        private readonly string empty2_path = "res/progress/empty_2.png";
        private readonly string empty1_path = "res/progress/empty_1.png";
        private readonly string emptyWarning_path = "res/progress/empty_0.png";
        private readonly string emptyWarningAlt_path = "res/progress/empty_0_alt.png";

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
            Connection.OnTitleParametersDidChange += Connection_OnTitleParametersDidChange;
        }

        #region Overrides

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        private void Connection_OnTitleParametersDidChange(object sender, SDEventReceivedEventArgs<TitleParametersDidChange> e)
        {
            titleParameters = e.Event?.Payload?.TitleParameters;
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
            Image image = GetEmptyImage();
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var title = "";
                var diff = (DateTime.Now - lastDrink);
                if (diff.TotalSeconds > settings.CycleLengthSeconds)
                {
                    title += "Time to drink!";
                }
                else
                {
                    title += "Drink in ";
                    var remaining = TimeSpan.FromSeconds(settings.CycleLengthSeconds) - diff;
                    if (remaining.TotalMinutes < 1)
                    {
                        title += "< 1min";
                    }
                    else
                    {
                        if (remaining.Hours > 1)
                        {
                            title += $"{remaining.Hours}hrs";
                        }
                        else if (remaining.Hours == 1)
                        {
                            title += "1hr";
                        }

                        if (remaining.Minutes > 1)
                        {
                            if (remaining.Hours > 0)
                            {
                                title += $", {remaining.Minutes}mins";
                            }
                            else
                            {
                                title += $"{remaining.Minutes}mins";
                            }
                        }
                        else if (remaining.Minutes == 1)
                        {
                            if (remaining.Hours > 0)
                            {
                                title += $", {remaining.Minutes}min";
                            }
                            else
                            {
                                title += $"{remaining.Minutes}min";
                            }
                        }

                    }
                }
                // TODO don't redo the params every draw, only when they change
                TitleParameters adaptedParams = new TitleParameters(
                    titleParameters.FontFamily,
                    titleParameters.FontStyle,
                    titleParameters.FontSizeInPoints,
                    titleParameters.TitleColor,
                    true,
                    TitleVerticalAlignment.Top
                );
                string splitTitle = Tools.SplitStringToFit(title, adaptedParams);
                graphics.AddTextPath(adaptedParams, image.Height, image.Width, splitTitle);
                await Connection.SetImageAsync(image);
            }
        }

        private async void DrawStats()
        {
            Image image = GetEmptyImage();
            using (Graphics graphics = Graphics.FromImage(image))
            {
                // TODO don't redo the params every draw, only when they change
                TitleParameters adaptedParams = new TitleParameters(
                    titleParameters.FontFamily,
                    titleParameters.FontStyle,
                    titleParameters.FontSizeInPoints,
                    titleParameters.TitleColor,
                    true,
                    TitleVerticalAlignment.Top
                );
                string splitTitle = Tools.SplitStringToFit($"Session:\n14\nTotal:\n1231", adaptedParams);
                graphics.AddTextPath(adaptedParams, image.Height, image.Width, splitTitle);
                await Connection.SetImageAsync(image);
            }
        }

        private Image GetEmptyImage()
        {
            switch ((DateTime.Now - lastDrink).TotalSeconds / settings.CycleLengthSeconds)
            {
                case double percentage when (percentage < 0.1):
                    return Image.FromFile(empty10_path, true);
                case double s when (s < 0.2):
                    return Image.FromFile(empty9_path, true);
                case double s when (s < 0.3):
                    return Image.FromFile(empty8_path, true);
                case double s when (s < 0.4):
                    return Image.FromFile(empty7_path, true);
                case double s when (s < 0.5):
                    return Image.FromFile(empty6_path, true);
                case double s when (s < 0.6):
                    return Image.FromFile(empty5_path, true);
                case double s when (s < 0.7):
                    return Image.FromFile(empty4_path, true);
                case double s when (s < 0.8):
                    return Image.FromFile(empty3_path, true);
                case double s when (s < 0.9):
                    return Image.FromFile(empty2_path, true);
                case double s when (s < 1):
                    return Image.FromFile(empty1_path, true);
                default:
                    Image image;
                    if (!altWarning)
                    {
                        image = Image.FromFile(emptyWarning_path, true);
                    }
                    else
                    {
                        image = Image.FromFile(emptyWarningAlt_path, true);
                    }
                    altWarning = !altWarning;
                    return image;
            }
        }
    }
}