namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 Appendix A.3 — Arithmetic functions for integer and double types.
/// </summary>
internal static class ArithmeticFunctions
{
    internal static void Register(DefaultFunctionRegistry registry)
    {
        RegisterIntegerArithmetic(registry);
        RegisterDoubleArithmetic(registry);
        RegisterRoundingFunctions(registry);
    }

    private static void RegisterIntegerArithmetic(DefaultFunctionRegistry registry)
    {
        registry.RegisterFunction(
            XACMLFunctionIds.IntegerAdd,
            XACMLDataTypes.Integer,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.IntegerAdd);
                var a = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.IntegerAdd, 0);
                var b = FunctionHelpers.CoerceToInt(args[1], XACMLFunctionIds.IntegerAdd, 1);
                return checked(a + b);
            });

        registry.RegisterFunction(
            XACMLFunctionIds.IntegerSubtract,
            XACMLDataTypes.Integer,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.IntegerSubtract);
                var a = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.IntegerSubtract, 0);
                var b = FunctionHelpers.CoerceToInt(args[1], XACMLFunctionIds.IntegerSubtract, 1);
                return checked(a - b);
            });

        registry.RegisterFunction(
            XACMLFunctionIds.IntegerMultiply,
            XACMLDataTypes.Integer,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.IntegerMultiply);
                var a = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.IntegerMultiply, 0);
                var b = FunctionHelpers.CoerceToInt(args[1], XACMLFunctionIds.IntegerMultiply, 1);
                return checked(a * b);
            });

        registry.RegisterFunction(
            XACMLFunctionIds.IntegerDivide,
            XACMLDataTypes.Integer,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.IntegerDivide);
                var a = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.IntegerDivide, 0);
                var b = FunctionHelpers.CoerceToInt(args[1], XACMLFunctionIds.IntegerDivide, 1);
                if (b == 0)
                {
                    throw new InvalidOperationException(
                        $"'{XACMLFunctionIds.IntegerDivide}': division by zero.");
                }

                return a / b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.IntegerMod,
            XACMLDataTypes.Integer,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.IntegerMod);
                var a = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.IntegerMod, 0);
                var b = FunctionHelpers.CoerceToInt(args[1], XACMLFunctionIds.IntegerMod, 1);
                if (b == 0)
                {
                    throw new InvalidOperationException(
                        $"'{XACMLFunctionIds.IntegerMod}': division by zero.");
                }

                return a % b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.IntegerAbs,
            XACMLDataTypes.Integer,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.IntegerAbs);
                var a = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.IntegerAbs, 0);
                return Math.Abs(a);
            });
    }

    private static void RegisterDoubleArithmetic(DefaultFunctionRegistry registry)
    {
        registry.RegisterFunction(
            XACMLFunctionIds.DoubleAdd,
            XACMLDataTypes.Double,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DoubleAdd);
                var a = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.DoubleAdd, 0);
                var b = FunctionHelpers.CoerceToDouble(args[1], XACMLFunctionIds.DoubleAdd, 1);
                return a + b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DoubleSubtract,
            XACMLDataTypes.Double,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DoubleSubtract);
                var a = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.DoubleSubtract, 0);
                var b = FunctionHelpers.CoerceToDouble(args[1], XACMLFunctionIds.DoubleSubtract, 1);
                return a - b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DoubleMultiply,
            XACMLDataTypes.Double,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DoubleMultiply);
                var a = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.DoubleMultiply, 0);
                var b = FunctionHelpers.CoerceToDouble(args[1], XACMLFunctionIds.DoubleMultiply, 1);
                return a * b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DoubleDivide,
            XACMLDataTypes.Double,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DoubleDivide);
                var a = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.DoubleDivide, 0);
                var b = FunctionHelpers.CoerceToDouble(args[1], XACMLFunctionIds.DoubleDivide, 1);
                if (Math.Abs(b) < double.Epsilon)
                {
                    throw new InvalidOperationException(
                        $"'{XACMLFunctionIds.DoubleDivide}': division by zero.");
                }

                return a / b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DoubleAbs,
            XACMLDataTypes.Double,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.DoubleAbs);
                var a = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.DoubleAbs, 0);
                return Math.Abs(a);
            });
    }

    private static void RegisterRoundingFunctions(DefaultFunctionRegistry registry)
    {
        registry.RegisterFunction(
            XACMLFunctionIds.Round,
            XACMLDataTypes.Double,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.Round);
                var a = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.Round, 0);
                return Math.Round(a, MidpointRounding.AwayFromZero);
            });

        registry.RegisterFunction(
            XACMLFunctionIds.Floor,
            XACMLDataTypes.Double,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.Floor);
                var a = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.Floor, 0);
                return Math.Floor(a);
            });
    }
}
