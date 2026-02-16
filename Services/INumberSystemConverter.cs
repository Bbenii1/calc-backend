using System;

namespace Calc.Backend.Services
{
    public interface INumberSystemConverter
    {
        string Convert(string value, int fromBase, int toBase);
    }
}
