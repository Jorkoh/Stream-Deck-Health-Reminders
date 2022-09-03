using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.jorkoh.health.reminders
{
    class CycleLength
    {
        [JsonProperty(PropertyName = "lengthName")]
        public string CycleLengthName { get; set; }

        [JsonProperty(PropertyName = "lengthSeconds")]
        public int CycleLengthSeconds { get; set; }
    }
}
