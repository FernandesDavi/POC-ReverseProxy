using System;

namespace WebApplication2
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }
    }

    public class AqueleTeste
    {
        public string url { get; set; }
        public string key { get; set; }
    }
}
