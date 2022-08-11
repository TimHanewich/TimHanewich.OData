using System;

namespace TimHanewich.OData
{
    public class ODataOrder
    {
        public string ColumnName {get; set;}
        public OrderDirection Direction {get; set;}

        public ODataOrder()
        {
            ColumnName = string.Empty;
        }

        public string ToODataString()
        {
            string ToReturn = ColumnName;
            if (Direction == OrderDirection.Ascending)
            {
                ToReturn = ToReturn + " asc";
            }
            else if (Direction == OrderDirection.Descending)
            {
                ToReturn = ToReturn + " desc";
            }
            return ToReturn;
        }
    }
}