using System;

namespace Calc.Backend.Services
{
    public class NumberSystemConverter : INumberSystemConverter
    {
        public string Convert(string value, int fromBase, int toBase)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be empty");

            if (!IsSupportedBase(fromBase) || !IsSupportedBase(toBase))
                throw new ArgumentException("Supported bases are 2, 8, 10, 16");

            try
            {
                // Convert to decimal (base 10) first
                long decimalValue = System.Convert.ToInt64(value, fromBase);

                // Convert from decimal to target base
                if (toBase == 10)
                    return decimalValue.ToString();

                string result = System.Convert.ToString(decimalValue, toBase).ToUpper();
                return result;
            }
            catch (FormatException)
            {
                throw new ArgumentException($"Value '{value}' is not valid for base {fromBase}");
            }
            catch (OverflowException)
            {
                 throw new ArgumentException($"Value '{value}' is too large to convert");
            }
        }

        private bool IsSupportedBase(int baseValue)
        {
            return baseValue == 2 || baseValue == 8 || baseValue == 10 || baseValue == 16;
        }
    }
}
