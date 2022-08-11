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

## Create Operation
Example of creating a `Contact` record via an OData request and converting the `ODataOperation` into an `HttpRequestMessage` that can be delivered to the OData endpoint:
```
ODataOperation op = new ODataOperation();
op.Operation = DataOperation.Create;
op.Resource = "Contact";

//Create the object with JSON that will be created
JObject jo = new JObject();
jo.Add("FirstName", "John");
jo.Add("LastName", "Appleseed");
jo.Add("DateOfBirth", new DateTime(1980, 1, 1));
op.Payload = jo;

//Create the HttpRequestMessage to send that contains your OData create operation
HttpRequestMessage req = op.ToHttpRequestMessage("https://my_site.com/my_odata_endpoint");
```

## Update Operation
Example update operation on a Contact record with primary key `e23fa96e-46ac-4b06-bac1-02cf0fe636b8`:
```
ODataOperation op = new ODataOperation();
op.Operation = DataOperation.Update;
op.Resource = "Contact";
op.RecordIdentifier = "e23fa96e-46ac-4b06-bac1-02cf0fe636b8";

//Create the payload that defines the properties that should be modified
JObject jo = new JObject();
jo.Add("Address", "101 Main Street");
op.Payload = jo;

//Create the HttpRequestMessage to send that contains your OData create operation
HttpRequestMessage req = op.ToHttpRequestMessage("https://my_site.com/my_odata_endpoint");
```

## Delete Operation
Example deletion of a Contact record with primary key `e23fa96e-46ac-4b06-bac1-02cf0fe636b8`:
```
ODataOperation op = new ODataOperation();
op.Operation = DataOperation.Delete;
op.Resource = "Contact";
op.RecordIdentifier = "e23fa96e-46ac-4b06-bac1-02cf0fe636b8";

//Create the HttpRequestMessage to send that contains your OData create operation
HttpRequestMessage req = op.ToHttpRequestMessage("https://my_site.com/my_odata_endpoint");
```

## Converting an OData HttpRequestMessage directly to SQL
Back-end API's playing the server role are often the medium that directly accept an OData query via an HTTP request, execute the query against the SQL database, and return the appropriate response to the HTTP requestor. This library is designed to simplify this use case by directly handling the "translation" from an HttpRequestMessage to a SQL query. 

Example converting the `HttpRequestMessage` from the **Read** operation example above:
```
//"req" is an OData-formatted HttpRequestMessage that the server has received
//Parsing the request:
ODataOperation op = ODataOperation.Parse(req);

//Convert to SQL
string sql_query = op.ToSql();
Console.WriteLine(sql_query);

//select top 25 FirstName,LastName from Contacts where Age >= '47' and LastName = 'Smith' order by DateOfBirth desc
```
*Update* and *Delete* OData operations require the SQL table's primary key to be referenced. In these cases, you will need to pass the name of the primary key column to the `ToSql()` method.

For example, on the **Update** operation example from above:
```
//"req" is an OData-formatted HttpRequestMessage that the server has received
//Parsing the request:
ODataOperation op = ODataOperation.Parse(req);

//Convert to SQL
string sql_query = op.ToSql("ContactID");
Console.WriteLine(sql_query);

//update Contact set Address = '101 Main Street' where ContactID = 'e23fa96e-46ac-4b06-bac1-02cf0fe636b8'
```
In the above example, `ContactID` is the primary key of the `Contact` table. If you do NOT specify the primary key but the `ToSql()` method requires it for a conversion, it will substitute the primary key with "<PRIMARY_KEY>":
```
update Contact set Address = '101 Main Street' where <PRIMARY_KEY> = 'e23fa96e-46ac-4b06-bac1-02cf0fe636b8'
```