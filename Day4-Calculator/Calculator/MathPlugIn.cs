using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Calculator
{
    // Step 1: Define the Native C# Plugin
    public class MathPlugIn
    {
        [KernelFunction("Add")]
        [Description("Adds two numbers together.")]
        public double Add([Description("The first number to add")] double number1, [Description("The second number to add")] double number2)
        {
            Console.WriteLine($"[NATIVE CODE EXECUTION] Adding {number1} + {number2}");
            return number1 + number2;
        }

        [KernelFunction("Subtract")]
        [Description("Subtracts the second number from the first number.")]
        public double Subtract([Description("The number to subtract from")] double number1, [Description("The number to subtract")] double number2)
        {
            Console.WriteLine($"[NATIVE CODE EXECUTION] Subtracting {number1} - {number2}");
            return number1 - number2;
        }

        [KernelFunction("Multiply")]
        [Description("Multiplies two numbers together.")]
        public double Multiply([Description("The first number to multiply")] double number1, [Description("The second number to multiply")] double number2)
        {
            Console.WriteLine($"[NATIVE CODE EXECUTION] Multiplying {number1} * {number2}");
            return number1 * number2;
        }

        [KernelFunction("Divide")]
        [Description("Divides the first number by the second number.")]
        public double Divide([Description("The number to divide")] double number1, [Description("The number to divide by")] double number2)
        {
            Console.WriteLine($"[NATIVE CODE EXECUTION] Dividing {number1} / {number2}");
            return number1 / number2;
        }
    }
}
