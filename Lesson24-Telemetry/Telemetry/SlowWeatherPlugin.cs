// SlowWeatherPlugin
// ---------------------------------------------------------------------------
// A mock plugin with a deliberate artificial delay, so its telemetry span
// shows up with a clearly distinguishable duration from the surrounding
// model-call spans when viewing the OpenTelemetry trace output.
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Telemetry
{
    /// <summary>Mock weather plugin with an artificial delay, so its telemetry span shows a clearly distinguishable duration.</summary>
    internal class SlowWeatherPlugin
    {
        /// <summary>Gets the current weather for a specific city, with an artificial delay to make its telemetry span stand out.</summary>
        /// <param name="city">The city name to look up weather for.</param>
        /// <returns>A short, simulated weather description for the requested city.</returns>
        [KernelFunction("GetWeather")]
        [Description("Gets the current weather for a specific city.")]
        public async Task<string> GetWeatherAsync([Description("The city name")] string city)
        {
            // Simulate network latency (e.g. reaching out to a weather REST API)
            await Task.Delay(800);
            return $"The weather in {city} is currently 72°F and sunny.";
        }
    }
}