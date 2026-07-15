using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Telementry
{
    internal class SlowWeatherPlugin
    {
        [KernelFunction("GetWeather")]
        [Description("Gets the current weather for a specificy city.")]
        public async Task<string> GetWeatherAsync([Description("The city name")] string city)
        {
            // Simulate network latency (e.g. reaching out to a weather REST API) 
            await Task.Delay(800);
            return $"The weather in {city} is currenlty 72°F and sunny.";
        }
    }
}