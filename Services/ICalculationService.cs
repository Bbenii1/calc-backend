using System;

namespace Calc.Backend.Services
{
    public interface ICalculationService
    {
        /// <summary>
        /// Evaluate a mathematical expression string in radian mode.
        /// Example: Calculate("2+3") → 5
        /// </summary>
        double Calculate(string expression);

        /// <summary>
        /// Evaluate a mathematical expression string with an explicit angle mode.
        /// <param name="angleMode">"deg" for degrees, "rad" for radians</param>
        /// Example: Calculate("sin(90)", "deg") → 1
        /// </summary>
        double Calculate(string expression, string angleMode);
    }
}
