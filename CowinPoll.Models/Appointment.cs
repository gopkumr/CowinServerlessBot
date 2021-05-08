using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace CowinPoll.Models
{
    public partial class Appointment
    {
        [JsonProperty("centers")]
        public Center[] Centers { get; set; }
    }

    public partial class Center
    {
        [JsonProperty("center_id")]
        public long CenterId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_l")]
        public string NameL { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("address_l")]
        public string AddressL { get; set; }

        [JsonProperty("state_name")]
        public string StateName { get; set; }

        [JsonProperty("state_name_l")]
        public string StateNameL { get; set; }

        [JsonProperty("district_name")]
        public string DistrictName { get; set; }

        [JsonProperty("district_name_l")]
        public string DistrictNameL { get; set; }

        [JsonProperty("block_name")]
        public string BlockName { get; set; }

        [JsonProperty("block_name_l")]
        public string BlockNameL { get; set; }

        [JsonProperty("pincode")]
        public long Pincode { get; set; }

        [JsonProperty("lat")]
        public double Lat { get; set; }

        [JsonProperty("long")]
        public double Long { get; set; }

        [JsonProperty("from")]
        public DateTimeOffset From { get; set; }

        [JsonProperty("to")]
        public DateTimeOffset To { get; set; }

        [JsonProperty("fee_type")]
        public string FeeType { get; set; }

        [JsonProperty("vaccine_fees")]
        public VaccineFee[] VaccineFees { get; set; }

        [JsonProperty("sessions")]
        public Session[] Sessions { get; set; }
    }

    public partial class Session
    {
        [JsonProperty("session_id")]
        public Guid SessionId { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("available_capacity")]
        public long AvailableCapacity { get; set; }

        [JsonProperty("min_age_limit")]
        public long MinAgeLimit { get; set; }

        [JsonProperty("vaccine")]
        public string Vaccine { get; set; }

        [JsonProperty("slots")]
        public string[] Slots { get; set; }
    }

    public partial class VaccineFee
    {
        [JsonProperty("vaccine")]
        public string Vaccine { get; set; }

        [JsonProperty("fee")]
        public long Fee { get; set; }
    }
}


