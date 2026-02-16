using Microsoft.AspNetCore.Mvc;
using Calc.Backend.Services;

namespace Calc.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalculatorController : ControllerBase
    {
        private readonly ICalculationService _calculationService;
        private readonly INumberSystemConverter _numberSystemConverter;

        public CalculatorController(ICalculationService calculationService, INumberSystemConverter numberSystemConverter)
        {
            _calculationService = calculationService;
            _numberSystemConverter = numberSystemConverter;
        }

        /// <summary>
        /// Evaluate a mathematical expression.
        /// </summary>
        /// <param name="expression">The expression string, e.g. "sin(pi/2)", "2^10", "ncr(5,2)"</param>
        /// <param name="angleMode">Angle mode: "rad" (default) or "deg"</param>
        /// <returns>The numeric result.</returns>
        /// <remarks>
        /// Example requests:
        ///   GET /api/calculator/calculate?expression=2%2B3              → { "result": 5 }
        ///   GET /api/calculator/calculate?expression=sin(90)&amp;angleMode=deg  → { "result": 1 }
        ///   GET /api/calculator/calculate?expression=ncr(5,2)           → { "result": 10 }
        /// </remarks>
        [HttpGet("calculate")]
        public IActionResult Calculate(
            [FromQuery] string expression,
            [FromQuery] string angleMode = "rad")
        {
            try
            {
                string decodedExpression = Uri.UnescapeDataString(expression);
                var result = _calculationService.Calculate(decodedExpression, angleMode);
                return Ok(new { result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("convert")]
        public IActionResult Convert([FromQuery] string value, [FromQuery] int fromBase, [FromQuery] int toBase)
        {
            try
            {
                var result = _numberSystemConverter.Convert(value, fromBase, toBase);
                return Ok(new { result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
