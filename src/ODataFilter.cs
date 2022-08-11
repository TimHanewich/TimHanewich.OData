using System;

namespace TimHanewich.OData
{
    public class ODataFilter
    {
        public LogicalOperator? LogicalOperatorPrefix {get; set;}
        public string ColumnName {get; set;}
        public ComparisonOperator Operator {get; set;}
        
        //Value to compare to
        private string _Value;
        public string Value
        {
            get
            {
                return _Value;
            }
        }

        public ODataFilter()
        {
            LogicalOperatorPrefix = null;
            ColumnName = string.Empty;
            _Value = string.Empty;
        }
    
        public void SetValue(string value)
        {
            _Value = ContainWithQuotesIfNeeded(value);
        }

        public void SetValue(float value)
        {
            _Value = value.ToString();
        }

        public void SetValue(int value)
        {
            _Value = value.ToString();
        }
    
        public void SetValue(Guid value)
        {
            _Value = "'" + value.ToString() + "'";
        }



        #region "String conversion"

        public string ToODataString()
        {
            if (ColumnName == null)
            {
                throw new Exception("Unable to convert CDS Read Filter to string. Column name was null.");
            }
            if (Value == null)
            {
                throw new Exception("Unable to convert CDS Read Filter to string. Value was null.");
            }

            //Prepare
            string ToReturn = ColumnName + " " + OperatorToString(Operator) + " " + Value;

            //Add the logical operator
            if (LogicalOperatorPrefix.HasValue)
            {
                ToReturn = LogicalOperatorToString(LogicalOperatorPrefix.Value) + " " + ToReturn;
            }

            return ToReturn;
        }

        public override string ToString()
        {
            return ToODataString();
        }

        #endregion

        #region "Utility Functions"

        private string OperatorToString(ComparisonOperator op)
        {
            switch (op)
            {
                case ComparisonOperator.Equals:
                    return "eq";
                case ComparisonOperator.GreaterThan:
                    return "gt";
                case ComparisonOperator.LessThan:
                    return "lt";
                case ComparisonOperator.NotEqualTo:
                    return "ne";
                case ComparisonOperator.GreaterThanOrEqualTo:
                    return "ge";
                case ComparisonOperator.LessThanOrEqualTo:
                    return "le";
                default:
                    throw new Exception("String operator unknown for '" + op.ToString() + "'");
            }
        }

        private string LogicalOperatorToString(LogicalOperator op)
        {
            switch (op)
            {
                case LogicalOperator.And:
                    return "and";
                case LogicalOperator.Or:
                    return "or";
                case LogicalOperator.Not:
                    return "not";
                default:
                    throw new Exception("No string value known for logical operator '" + op.ToString() + "'");
            }
        }

        private bool ContainedByQuotes(string s)
        {
            string fChar = s.Substring(0, 1);
            string lChar = s.Substring(s.Length - 1, 1);
            if ((fChar == "'" && lChar == "'") || (fChar == "\"" && lChar == "\""))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string ContainWithQuotesIfNeeded(string s)
        {
            if (s == null)
            {
                return "''";
            }
            else
            {
                if (ContainedByQuotes(s) == false)
                {
                    return "'" + s + "'";
                }
                else
                {
                    return s;
                }
            }
        }

        #endregion
    }
}