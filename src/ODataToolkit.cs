using System;

namespace TimHanewich.OData
{
    public static class ODataToolkit
    {
        public static string ToSymbol(this ComparisonOperator op)
        {
            switch (op)
            {
                case ComparisonOperator.Equals:
                    return "=";
                case ComparisonOperator.GreaterThan:
                    return ">";
                case ComparisonOperator.LessThan:
                    return "<";
                case ComparisonOperator.GreaterThanOrEqualTo:
                    return ">=";
                case ComparisonOperator.LessThanOrEqualTo:
                    return "<=";
                case ComparisonOperator.NotEqualTo:
                    return "!=";
                default:
                    throw new Exception("I do not know a symbol for ComparisonOperator '" + op.ToString() + "'");
            }
        }
    }
}