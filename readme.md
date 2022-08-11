# TimHanewich.OData
**TimHanewich.OData** is a lightweight .NET library for parsing, composing, and translating OData operations. [OData, short for "Open Data Protocol"](https://www.odata.org/) allows the creation and consumption of queryable and interoperable REST APIs in a simple and standard way. Microsoft originally initiated OData in 2007 and OData has become the industry-embraced standard since.

This library is designed to assist with the following:
1. Constructing an OData query and converting this to an `HttpRequestMessage`
2. Deconstructing an OData `HttpRequestMessage` into it's various components
3. Translating an OData query to it's SQL equivalent that can be executed against a SQL database 

## Read Operation
Example read operation with $top, $filter, $select, $order:
```
ODataOperation o = new ODataOperation();
o.Operation = DataOperation.Read;

//Set table name and limit # of records
o.Resource = "Contacts"; //the table/resource name you are trying to read from
o.top = 25;

//Select certain fields
o.select.Add("FirstName");
o.select.Add("LastName");

//Add filter - LastName
ODataFilter filter1 = new ODataFilter();
filter1.ColumnName = "LastName";
filter1.Operator = ComparisonOperator.Equals;
filter1.SetValue("Smith");
o.filter.Add(filter1);

//Add filter - Age
ODataFilter filter2 = new ODataFilter();
filter1.LogicalOperatorPrefix = LogicalOperator.And; //Add "and" to this, to imply this is in addition to filter1
filter2.ColumnName = "Age";
filter2.Operator = ComparisonOperator.GreaterThanOrEqualTo;
filter2.SetValue(47);
o.filter.Add(filter2);

//Order (sort) results
ODataOrder order = new ODataOrder();
order.ColumnName = "DateOfBirth";
order.Direction = OrderDirection.Descending;
o.orderby.Add(order);

Console.WriteLine(o.ToHttpRequestMessage("https://my_site.com/my_odata_endpoint/").RequestUri.ToString());

//https://my_site.com/my_odata_endpoint/Contacts?$filter=Age+ge+47+and+LastName+eq+%27Smith%27&$select=FirstName%2cLastName&$orderby=DateOfBirth+desc&$top=25
```