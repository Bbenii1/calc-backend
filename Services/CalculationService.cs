using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Calc.Backend.Services
{
    // ═══════════════════════════════════════════════════════════════════════
    //  CalculationService – Scientific Expression Evaluator
    // ═══════════════════════════════════════════════════════════════════════
    //
    //  Receives a mathematical expression as a string and evaluates it.
    //  Pipeline: Tokenizer → Shunting-Yard (infix → RPN) → RPN Evaluator
    //
    //  ── Supported Syntax & Test Cases ──────────────────────────────────
    //
    //  Core Arithmetic
    //    "2+3"           → 5        (addition)
    //    "10-4"          → 6        (subtraction)
    //    "3*7"           → 21       (multiplication)
    //    "20/4"          → 5        (division)
    //    "neg(5)"        → -5       (sign change / negation)
    //    "(2+3)*4"       → 20       (parentheses)
    //    "50%"           → 0.5      (percentage)
    //    "-3+5"          → 2        (unary minus)
    //
    //  Powers & Roots
    //    "sq(4)"         → 16       (x²)
    //    "cube(3)"       → 27       (x³)
    //    "2^10"          → 1024     (xʸ via operator)
    //    "pow(2,10)"     → 1024     (xʸ via function)
    //    "sqrt(16)"      → 4        (√x)
    //    "cbrt(27)"      → 3        (∛x)
    //    "root(3,27)"    → 3        (ʸ√x – nth root)
    //    "inv(4)"        → 0.25     (1/x – reciprocal)
    //
    //  Exponentials & Logarithms
    //    "exp(1)"        → 2.71828… (eˣ)
    //    "exp10(3)"      → 1000     (10ˣ)
    //    "ln(e)"         → 1        (natural logarithm)
    //    "log(100)"      → 2        (base-10 logarithm)
    //
    //  Trigonometric (default angleMode = "rad")
    //    "sin(pi/2)"     → 1        (sine, radians)
    //    "cos(0)"        → 1        (cosine)
    //    "tan(pi/4)"     → 1        (tangent)
    //    "asin(1)"       → 1.5707…  (inverse sine → π/2)
    //    "acos(1)"       → 0        (inverse cosine)
    //    "atan(1)"       → 0.7853…  (inverse tangent → π/4)
    //
    //  Trigonometric (angleMode = "deg")
    //    "sin(90)"       → 1
    //    "cos(0)"        → 1
    //    "tan(45)"       → 1
    //    "asin(1)"       → 90
    //    "acos(1)"       → 0
    //    "atan(1)"       → 45
    //
    //  Hyperbolic
    //    "sinh(0)"       → 0
    //    "cosh(0)"       → 1
    //    "tanh(0)"       → 0
    //    "asinh(0)"      → 0
    //    "acosh(1)"      → 0
    //    "atanh(0)"      → 0
    //
    //  Factorials & Combinatorics
    //    "5!"            → 120      (factorial)
    //    "npr(5,2)"      → 20       (permutations)
    //    "ncr(5,2)"      → 10       (combinations)
    //
    //  Constants
    //    "pi"            → 3.14159…
    //    "e"             → 2.71828…
    //
    //  Complex expressions
    //    "sqrt(sq(3)+sq(4))"         → 5      (Pythagorean)
    //    "log(exp10(5))"             → 5
    //    "ncr(10,3)+npr(4,2)"        → 132    (120 + 12)
    //    "2^(3+1)"                   → 16
    //    "sin(pi/6)*2"               → 1
    // ═══════════════════════════════════════════════════════════════════════

    public class CalculationService : ICalculationService
    {
        // ── Token types ────────────────────────────────────────────────
        private enum TokenType
        {
            Number,
            Function,
            Operator,
            LeftParen,
            RightParen,
            Comma,
            UnaryMinus,
            UnaryPlus
        }

        private class Token
        {
            public TokenType Type { get; set; }
            public string Value { get; set; } = "";
        }

        // ── Known names ────────────────────────────────────────────────

        // Unary (1-argument) functions
        private static readonly HashSet<string> UnaryFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "sin", "cos", "tan",
            "asin", "acos", "atan",
            "sinh", "cosh", "tanh",
            "asinh", "acosh", "atanh",
            "ln", "log",
            "exp", "exp10",
            "sqrt", "cbrt",
            "abs", "neg", "inv",
            "sq", "cube"
        };

        // Binary (2-argument) functions – called as func(a,b)
        private static readonly HashSet<string> BinaryFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "pow", "root", "npr", "ncr"
        };

        // Constants
        private static readonly Dictionary<string, double> Constants = new(StringComparer.OrdinalIgnoreCase)
        {
            { "pi", Math.PI },
            { "e",  Math.E  }
        };

        // ════════════════════════════════════════════════════════════════
        //  Public API
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Evaluate an expression in radian mode.
        /// Example: Calculate("sin(pi/2)") → 1
        /// </summary>
        public double Calculate(string expression)
        {
            return Calculate(expression, "deg");
        }

        /// <summary>
        /// Evaluate an expression with an explicit angle mode.
        /// <param name="angleMode">"deg" for degrees, "rad" for radians (default)</param>
        /// Example: Calculate("sin(90)", "deg") → 1
        /// </summary>
        public double Calculate(string expression, string angleMode)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return 0;

            try
            {
                var tokens = Tokenize(expression);
                var rpn = ShuntingYard(tokens);
                return EvaluateRPN(rpn, angleMode);
            }
            catch (Exception ex) when (ex is not ArgumentException and not DivideByZeroException)
            {
                throw new ArgumentException($"Invalid expression: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  1.  TOKENIZER
        // ════════════════════════════════════════════════════════════════
        //  Scans the raw string into a flat list of typed tokens.
        //  Handles implicit multiplication:  2pi → 2*pi,  )( → )*(

        private List<Token> Tokenize(string expression)
        {
            var tokens = new List<Token>();
            expression = expression.Replace(" ", "");
            int i = 0;

            while (i < expression.Length)
            {
                char c = expression[i];

                // ── Numbers (including decimals) ────────────────────
                if (char.IsDigit(c) || (c == '.' && i + 1 < expression.Length && char.IsDigit(expression[i + 1])))
                {
                    // Implicit multiplication: number right after ')' or after a constant/postfix
                    if (tokens.Count > 0 && NeedsImplicitMul(tokens[^1]))
                        tokens.Add(Op("*"));

                    var sb = new StringBuilder();
                    while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                    {
                        sb.Append(expression[i]);
                        i++;
                    }
                    tokens.Add(new Token { Type = TokenType.Number, Value = sb.ToString() });
                    continue;
                }

                // ── Letters → function names or constants ───────────
                if (char.IsLetter(c))
                {
                    // Implicit multiplication: letter right after ')' or number
                    if (tokens.Count > 0 && NeedsImplicitMul(tokens[^1]))
                        tokens.Add(Op("*"));

                    var sb = new StringBuilder();
                    while (i < expression.Length && (char.IsLetter(expression[i]) || char.IsDigit(expression[i])))
                    {
                        sb.Append(expression[i]);
                        i++;
                    }
                    string name = sb.ToString().ToLower(CultureInfo.InvariantCulture);

                    if (Constants.ContainsKey(name))
                        tokens.Add(new Token { Type = TokenType.Number, Value = Constants[name].ToString(CultureInfo.InvariantCulture) });
                    else if (UnaryFunctions.Contains(name) || BinaryFunctions.Contains(name))
                        tokens.Add(new Token { Type = TokenType.Function, Value = name });
                    else
                        throw new ArgumentException($"Unknown identifier: {name}");

                    continue;
                }

                // ── Parentheses ────────────────────────────────────
                if (c == '(')
                {
                    // Implicit multiplication: ( right after number, constant, or ')'
                    if (tokens.Count > 0 && NeedsImplicitMul(tokens[^1]))
                        tokens.Add(Op("*"));

                    tokens.Add(new Token { Type = TokenType.LeftParen });
                    i++;
                    continue;
                }
                if (c == ')')
                {
                    tokens.Add(new Token { Type = TokenType.RightParen });
                    i++;
                    continue;
                }

                // ── Comma ──────────────────────────────────────────
                if (c == ',')
                {
                    tokens.Add(new Token { Type = TokenType.Comma });
                    i++;
                    continue;
                }

                // ── Postfix: ! (factorial) and % (percentage) ──────
                if (c == '!')
                {
                    tokens.Add(new Token { Type = TokenType.Operator, Value = "!" });
                    i++;
                    continue;
                }
                if (c == '%')
                {
                    tokens.Add(new Token { Type = TokenType.Operator, Value = "%" });
                    i++;
                    continue;
                }

                // ── Operators: + - * / ^ ───────────────────────────
                if (c == '+' || c == '-')
                {
                    // Determine if this is unary or binary
                    bool isUnary = tokens.Count == 0
                        || tokens[^1].Type == TokenType.LeftParen
                        || tokens[^1].Type == TokenType.Comma
                        || (tokens[^1].Type == TokenType.Operator && tokens[^1].Value != "!" && tokens[^1].Value != "%");

                    if (isUnary)
                    {
                        tokens.Add(new Token
                        {
                            Type = c == '-' ? TokenType.UnaryMinus : TokenType.UnaryPlus,
                            Value = c.ToString()
                        });
                    }
                    else
                    {
                        tokens.Add(Op(c.ToString()));
                    }
                    i++;
                    continue;
                }

                if (c == '*' || c == '/' || c == '^')
                {
                    tokens.Add(Op(c.ToString()));
                    i++;
                    continue;
                }

                // Unknown character – skip whitespace, throw on everything else
                if (!char.IsWhiteSpace(c))
                    throw new ArgumentException($"Unexpected character: {c}");
                i++;
            }

            return tokens;
        }

        // Helper: does the previous token require an implicit '*' before the next value?
        private static bool NeedsImplicitMul(Token prev)
        {
            return prev.Type == TokenType.Number
                || prev.Type == TokenType.RightParen
                || prev.Value == "!" || prev.Value == "%";
        }

        private static Token Op(string value) => new() { Type = TokenType.Operator, Value = value };

        // ════════════════════════════════════════════════════════════════
        //  2.  SHUNTING-YARD  (infix tokens → RPN queue)
        // ════════════════════════════════════════════════════════════════

        private Queue<Token> ShuntingYard(List<Token> tokens)
        {
            var output = new Queue<Token>();
            var ops = new Stack<Token>();

            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case TokenType.Number:
                        output.Enqueue(token);
                        break;

                    case TokenType.Function:
                        ops.Push(token);
                        break;

                    case TokenType.Comma:
                        // Pop until '(' for the next function argument
                        while (ops.Count > 0 && ops.Peek().Type != TokenType.LeftParen)
                            output.Enqueue(ops.Pop());
                        break;

                    case TokenType.UnaryMinus:
                    case TokenType.UnaryPlus:
                        // Treat as highest-precedence right-associative unary prefix
                        ops.Push(token);
                        break;

                    case TokenType.Operator:
                        // Postfix operators (! and %) go straight to output
                        if (token.Value == "!" || token.Value == "%")
                        {
                            output.Enqueue(token);
                            break;
                        }

                        while (ops.Count > 0 && ops.Peek().Type == TokenType.Operator
                            && ops.Peek().Value != "!" && ops.Peek().Value != "%"
                            && ShouldPopOperator(ops.Peek().Value, token.Value))
                        {
                            output.Enqueue(ops.Pop());
                        }
                        ops.Push(token);
                        break;

                    case TokenType.LeftParen:
                        ops.Push(token);
                        break;

                    case TokenType.RightParen:
                        while (ops.Count > 0 && ops.Peek().Type != TokenType.LeftParen)
                            output.Enqueue(ops.Pop());

                        if (ops.Count == 0)
                            throw new ArgumentException("Mismatched parentheses");

                        ops.Pop(); // pop '('

                        // If a function sits on top, pop it to output
                        if (ops.Count > 0 && ops.Peek().Type == TokenType.Function)
                            output.Enqueue(ops.Pop());
                        break;
                }
            }

            while (ops.Count > 0)
            {
                if (ops.Peek().Type == TokenType.LeftParen)
                    throw new ArgumentException("Mismatched parentheses");
                output.Enqueue(ops.Pop());
            }

            return output;
        }

        // Precedence & associativity rules for binary operators
        private static int Precedence(string op) => op switch
        {
            "+" or "-" => 2,
            "*" or "/" => 3,
            "^" => 4,
            _ => 0
        };

        private static bool IsRightAssociative(string op) => op == "^";

        private static bool ShouldPopOperator(string top, string current)
        {
            int tp = Precedence(top), cp = Precedence(current);
            return tp > cp || (tp == cp && !IsRightAssociative(current));
        }

        // ════════════════════════════════════════════════════════════════
        //  3.  RPN EVALUATOR
        // ════════════════════════════════════════════════════════════════

        private double EvaluateRPN(Queue<Token> rpn, string angleMode)
        {
            var stack = new Stack<double>();
            bool useDeg = angleMode.Equals("deg", StringComparison.OrdinalIgnoreCase);

            while (rpn.Count > 0)
            {
                var token = rpn.Dequeue();

                switch (token.Type)
                {
                    // ── Literal number → push on stack ──────────────
                    case TokenType.Number:
                        stack.Push(double.Parse(token.Value, CultureInfo.InvariantCulture));
                        break;

                    // ── Unary +/- (prefix) ──────────────────────────
                    case TokenType.UnaryMinus:
                        EnsureStack(stack, 1, "unary -");
                        stack.Push(-stack.Pop());
                        break;

                    case TokenType.UnaryPlus:
                        EnsureStack(stack, 1, "unary +");
                        // value stays the same; pop & push is a no-op
                        break;

                    // ── Binary & postfix operators ───────────────────
                    case TokenType.Operator:
                        EvalOperator(stack, token.Value);
                        break;

                    // ── Functions ────────────────────────────────────
                    case TokenType.Function:
                        EvalFunction(stack, token.Value, useDeg);
                        break;

                    default:
                        throw new InvalidOperationException($"Unexpected token in RPN: {token.Value}");
                }
            }

            if (stack.Count != 1)
                throw new InvalidOperationException("Invalid expression – leftover values on stack");

            return stack.Pop();
        }

        // ── Operators ──────────────────────────────────────────────────

        private void EvalOperator(Stack<double> stack, string op)
        {
            // Postfix unary: ! and %
            if (op == "!")
            {
                EnsureStack(stack, 1, "!");
                stack.Push(Factorial(stack.Pop()));
                return;
            }
            if (op == "%")
            {
                // Example: "50%" → 0.5
                EnsureStack(stack, 1, "%");
                stack.Push(stack.Pop() / 100.0);
                return;
            }

            // Binary operators
            EnsureStack(stack, 2, op);
            double b = stack.Pop();
            double a = stack.Pop();

            switch (op)
            {
                case "+": stack.Push(a + b); break;
                case "-": stack.Push(a - b); break;
                case "*": stack.Push(a * b); break;
                case "/":
                    if (b == 0) throw new DivideByZeroException("Division by zero");
                    stack.Push(a / b);
                    break;
                case "^":
                    // Example: "2^10" → 1024
                    stack.Push(Math.Pow(a, b));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown operator: {op}");
            }
        }

        // ── Functions ──────────────────────────────────────────────────

        private void EvalFunction(Stack<double> stack, string name, bool useDeg)
        {
            // ── Binary functions (2-argument) ──────────────────────
            if (BinaryFunctions.Contains(name))
            {
                EnsureStack(stack, 2, name);
                double b = stack.Pop();
                double a = stack.Pop();

                switch (name)
                {
                    // pow(base, exponent)  –  Example: pow(2,10) → 1024
                    case "pow":
                        stack.Push(Math.Pow(a, b));
                        break;

                    // root(n, x)  –  nth root of x  –  Example: root(3,27) → 3
                    case "root":
                        if (a == 0) throw new ArgumentException("Root index cannot be zero");
                        stack.Push(Math.Pow(b, 1.0 / a));
                        break;

                    // npr(n, r)  –  Permutations  –  Example: npr(5,2) → 20
                    case "npr":
                        stack.Push(Permutation(a, b));
                        break;

                    // ncr(n, r)  –  Combinations  –  Example: ncr(5,2) → 10
                    case "ncr":
                        stack.Push(Combination(a, b));
                        break;
                }
                return;
            }

            // ── Unary functions (1-argument) ───────────────────────
            EnsureStack(stack, 1, name);
            double x = stack.Pop();

            switch (name)
            {
                // ── Trigonometric ──────────────────────────────────
                // Example (rad): sin(pi/2) → 1        cos(0) → 1        tan(pi/4) → 1
                // Example (deg): sin(90) → 1           cos(0) → 1        tan(45) → 1
                case "sin":
                    stack.Push(Math.Sin(ToRadians(x, useDeg)));
                    break;
                case "cos":
                    stack.Push(Math.Cos(ToRadians(x, useDeg)));
                    break;
                case "tan":
                    stack.Push(Math.Tan(ToRadians(x, useDeg)));
                    break;

                // ── Inverse trigonometric ──────────────────────────
                // Example (rad): asin(1) → π/2 ≈ 1.5708
                // Example (deg): asin(1) → 90
                case "asin":
                    stack.Push(FromRadians(Math.Asin(x), useDeg));
                    break;
                case "acos":
                    stack.Push(FromRadians(Math.Acos(x), useDeg));
                    break;
                case "atan":
                    stack.Push(FromRadians(Math.Atan(x), useDeg));
                    break;

                // ── Hyperbolic ─────────────────────────────────────
                // Example: sinh(0) → 0    cosh(0) → 1    tanh(0) → 0
                case "sinh":
                    stack.Push(Math.Sinh(x));
                    break;
                case "cosh":
                    stack.Push(Math.Cosh(x));
                    break;
                case "tanh":
                    stack.Push(Math.Tanh(x));
                    break;

                // ── Inverse hyperbolic ─────────────────────────────
                // Example: asinh(0) → 0   acosh(1) → 0   atanh(0) → 0
                case "asinh":
                    stack.Push(Math.Asinh(x));
                    break;
                case "acosh":
                    stack.Push(Math.Acosh(x));
                    break;
                case "atanh":
                    stack.Push(Math.Atanh(x));
                    break;

                // ── Logarithms ─────────────────────────────────────
                // Example: ln(e) → 1      log(100) → 2
                case "ln":
                    if (x <= 0) throw new ArgumentException("ln requires a positive argument");
                    stack.Push(Math.Log(x));
                    break;
                case "log":
                    if (x <= 0) throw new ArgumentException("log requires a positive argument");
                    stack.Push(Math.Log10(x));
                    break;

                // ── Exponentials ───────────────────────────────────
                // Example: exp(1) → 2.71828…     exp10(3) → 1000
                case "exp":
                    stack.Push(Math.Exp(x));
                    break;
                case "exp10":
                    stack.Push(Math.Pow(10, x));
                    break;

                // ── Roots ──────────────────────────────────────────
                // Example: sqrt(16) → 4           cbrt(27) → 3
                case "sqrt":
                    if (x < 0) throw new ArgumentException("sqrt requires a non-negative argument");
                    stack.Push(Math.Sqrt(x));
                    break;
                case "cbrt":
                    stack.Push(Math.Cbrt(x));
                    break;

                // ── Powers ─────────────────────────────────────────
                // Example: sq(4) → 16             cube(3) → 27
                case "sq":
                    stack.Push(x * x);
                    break;
                case "cube":
                    stack.Push(x * x * x);
                    break;

                // ── Misc ───────────────────────────────────────────
                // Example: abs(-7) → 7
                case "abs":
                    stack.Push(Math.Abs(x));
                    break;

                // Example: neg(5) → -5     neg(-3) → 3
                case "neg":
                    stack.Push(-x);
                    break;

                // Example: inv(4) → 0.25   (1/x – reciprocal)
                case "inv":
                    if (x == 0) throw new DivideByZeroException("inv(0) – division by zero");
                    stack.Push(1.0 / x);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown function: {name}");
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  4.  HELPER METHODS
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Factorial of a non-negative integer.
        /// Example: Factorial(5) → 120,  Factorial(0) → 1
        /// </summary>
        private static double Factorial(double n)
        {
            if (n < 0 || n != Math.Floor(n))
                throw new ArgumentException("Factorial requires a non-negative integer");
            double result = 1;
            for (int i = 2; i <= (int)n; i++)
                result *= i;
            return result;
        }

        /// <summary>
        /// Permutations: nPr = n! / (n-r)!
        /// Example: Permutation(5, 2) → 20
        /// </summary>
        private static double Permutation(double n, double r)
        {
            if (n < 0 || r < 0 || n != Math.Floor(n) || r != Math.Floor(r))
                throw new ArgumentException("nPr requires non-negative integers");
            if (r > n)
                throw new ArgumentException("nPr requires r ≤ n");
            return Factorial(n) / Factorial(n - r);
        }

        /// <summary>
        /// Combinations: nCr = n! / (r! * (n-r)!)
        /// Example: Combination(5, 2) → 10
        /// </summary>
        private static double Combination(double n, double r)
        {
            if (n < 0 || r < 0 || n != Math.Floor(n) || r != Math.Floor(r))
                throw new ArgumentException("nCr requires non-negative integers");
            if (r > n)
                throw new ArgumentException("nCr requires r ≤ n");
            return Factorial(n) / (Factorial(r) * Factorial(n - r));
        }

        /// <summary>
        /// Convert angle to radians if the calculator is in degree mode.
        /// Example (deg): ToRadians(90, true) → π/2
        /// </summary>
        private static double ToRadians(double angle, bool useDeg)
        {
            return useDeg ? angle * Math.PI / 180.0 : angle;
        }

        /// <summary>
        /// Convert radian result to degrees if the calculator is in degree mode.
        /// Example (deg): FromRadians(π/2, true) → 90
        /// </summary>
        private static double FromRadians(double radians, bool useDeg)
        {
            return useDeg ? radians * 180.0 / Math.PI : radians;
        }

        /// <summary>Ensure the evaluation stack has enough values.</summary>
        private static void EnsureStack(Stack<double> stack, int required, string context)
        {
            if (stack.Count < required)
                throw new InvalidOperationException($"Not enough operands for '{context}'");
        }
    }
}
