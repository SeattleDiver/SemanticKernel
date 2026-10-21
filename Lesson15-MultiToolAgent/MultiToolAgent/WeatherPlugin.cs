using System;
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace MultiToolAgent
{
    /// <summary>Native plugin exposing a (simulated) current-weather lookup as a Semantic Kernel tool.</summary>
    public class WeatherPlugin
    {
        /// <summary>Gets the current weather for a specific city.</summary>
        /// <param name="city">The city name to look up weather for.</param>
        /// <returns>A short weather description for the requested city.</returns>
        [KernelFunction("GetWeather")]
        [Description("Gets the current weather for a specific city.")]
        public string GetWeather([Description("The city name")] string city)
        {
            Console.WriteLine($"[EXECUTING TOOL] GetWeather: City {city}.");
            return city.Contains("London") ? "15°C and Rainy" : "22°C and Sunny";
        }
    }
}
