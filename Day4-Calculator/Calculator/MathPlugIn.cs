// MathPlugin
// ---------------------------------------------------------------------------
// A native C# plugin exposing basic arithmetic to the model. The
// [KernelFunction] attribute exposes each method to Semantic Kernel; the
// [Description] attributes are not just documentation for humans - Semantic
// Kernel sends them to the model as a "tool manifest" so it knows which
// function to call and what arguments to pass.
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Calculator
{
    // Step 1: Define the Native C# Plugin
    /// <summary>Native C# plugin exposing basic arithmetic operations as Semantic Kernel tools.</summary>
    public class MathPlugin
    {
        /// <summary>Adds two numbers together.</summary>
        /// <param name="number1">The first number to add.</param>
        /// <param name="number2">The second number to add.</param>
        /// <returns>The sum of <paramref name="number1"/> and <paramref name="number2"/>.</returns>
        [KernelFunction("Add")]
        [Description("Adds two numbers together.")]
        public double Add([Description("The first number to add")] double number1, [Description("The second number to add")] double number2)
        {
            Console.WriteLine($"[NATIVE CODE EXECUTION] Adding {number1} + {number2}");
            return number1 + number2;
        }

        /// <summary>Subtracts the second number from the first number.</summary>
        /// <param name="number1">The number to subtract from.</param>
        /// <param name="number2">The number to subtract.</param>
        /// <returns>The result of <paramref name="number1"/> minus <paramref name="number2"/>.</returns>
        [KernelFunction("Subtract")]
        [Description("Subtracts the second number from the first number.")]
        public double Subtract([Description("The number to subtract from")] double number1, [Description("The number to subtract")] double number2)
        {
            Console.WriteLine($"[NATIVE CODE EXECUTION] Subtracting {number1} - {number2}");
            return number1 - number2;
        }

        /// <summary>Multiplies two numbers together.</summary>
        /// <param name="number1">The first number to multiply.</param>
        /// <param name="number2">The second number to multiply.</param>
        /// <returns>The product of <paramref name="number1"/> and <paramref name="number2"/>.</returns>
        [KernelFunction("Multiply")]
        [Description("Multiplies two numbers together.")]
        public double Multiply([Description("The first number to multiply")] double number1, [Description("The second number to multiply")] double number2)
        {
            Console.WriteLine($"[NATIVE CODE EXECUTION] Multiplying {number1} * {number2}");
            return number1 * number2;
        }

        /// <summary>Divides the first number by the second number.</summary>
        /// <param name="number1">The number to divide.</param>
        /// <param name="number2">The number to divide by.</param>
        /// <returns>The result of <paramref name="number1"/> divided by <paramref name="number2"/>.</returns>
        /// <exception cref="DivideByZeroException">Thrown when <paramref name="number2"/> is zero.</exception>
        [KernelFunction("Divide")]
        [Description("Divides the first number by the second number.")]
        public double Divide([Description("The number to divide")] double number1, [Description("The number to divide by")] double number2)
        {
            // Guard: double division by zero doesn't throw, it silently returns
            // Infinity/NaN. Throw a descriptive exception instead so Semantic
            // Kernel can surface a real tool error back to the model.
            if (number2 == 0)
            {
                throw new DivideByZeroException("Cannot divide by zero.");
            }

            Console.WriteLine($"[NATIVE CODE EXECUTION] Dividing {number1} / {number2}");
            return number1 / number2;
        }
    }
}
