using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.jorkoh.health.reminders
{
    class ReminderTypeItem
    {
        [JsonProperty(PropertyName = "reminderTypeItemName")]
        public string ReminderTypeItemName { get; set; }

        [JsonProperty(PropertyName = "reminderTypeItemId")]
        public int ReminderTypeItemId { get; set; }
    }

    enum ReminderType {
        Water, Vision, Stretch
    }
}
