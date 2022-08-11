using System;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Specialized;
using TimHanewich.Sql;
using System.Linq;
using System.Text;

namespace TimHanewich.OData
{
    public class ODataOperation
    {
        
        //Settings
        private ODataSettings _Settings;
        public ODataSettings Settings
        {
            get
            {
                if (_Settings == null)
                {
                    _Settings = new ODataSettings();
                }
                return _Settings;
            }
        }

        #region "Query params"

        //Query intention (i.e. Creating, updating, reading ,deleting)
        public DataOperation Operation {get; set;}

        //Record Id (if it is an update and there is a specific record to update)
        public string RecordIdentifier {get; set;}

        //Body (JSON)
        public JObject? Payload {get; set;}

        //Resource (table name)
        public string Resource {get; set;}

        //select
        public List<string> select {get; set;}

        //filter
        public List<ODataFilter> filter {get; set;}
        
        //orderby
        public List<ODataOrder> orderby {get; set;}

        //top
        public int? top {get; set;} = null;

        //skip
        public int? skip {get; set;} = null;

        //count
        public bool count {get; set;} = false;
    

        #endregion
    
        #region "Constructors"
        
        public ODataOperation()
        {
            Resource = string.Empty;
            _Settings = new ODataSettings();
            Payload = null;
            RecordIdentifier = string.Empty;
            select = new List<string>();
            filter = new List<ODataFilter>();
            orderby = new List<ODataOrder>();
        }

        public static ODataOperation Parse(string url)
        {
            Uri u;
            try
            {
                u = new Uri(url);
            }
            catch (Exception ex)
            {
                throw new Exception("The provided string is not a valid URI: " + ex.Message);
            }

            return ODataOperation.Parse(u);
        }

        public static ODataOperation Parse(Uri path)
        {

            ODataOperation ToReturn = new ODataOperation();

            //Default
            ToReturn.Operation = DataOperation.Read;

            //Get the resource name
            string AbsPath = path.AbsolutePath;
            AbsPath = AbsPath.Replace("%20", " ");
            int lastForwardSlashLoc = AbsPath.LastIndexOf("/");
            if (lastForwardSlashLoc != -1)
            {
                int StartParanthesis = AbsPath.LastIndexOf("(");
                if (StartParanthesis == -1)
                {
                    string resourceTitle = AbsPath.Substring(lastForwardSlashLoc+1);
                    ToReturn.Resource = resourceTitle;
                }
                else
                {
                    string resourceTitle = AbsPath.Substring(lastForwardSlashLoc + 1, StartParanthesis - lastForwardSlashLoc - 1);
                    ToReturn.Resource = resourceTitle;
                }      
            }

            //Record identifier
            if (AbsPath.Substring(AbsPath.Length-1, 1) == ")")
            {
                int pOpen = AbsPath.LastIndexOf("(");
                int pClose = AbsPath.LastIndexOf(")");
                string rID = AbsPath.Substring(pOpen + 1, pClose - pOpen - 1);
                ToReturn.RecordIdentifier = rID;
            }

            //Get the query portion
            NameValueCollection nvc = HttpUtility.ParseQueryString(path.Query);

            //Loop through each parameter and parse
            foreach (string key in nvc.Keys)
            {
                string? value = nvc.Get(key);

                if (value != null)
                {
                    //select
                    if (key.ToLower() == "$select")
                    {
                        string[] columns = value.Split(new string[]{","}, StringSplitOptions.RemoveEmptyEntries);
                        ToReturn.select.Clear();
                        ToReturn.select.AddRange(columns);
                    }

                    //filter
                    if (key.ToLower() == "$filter")
                    {
                        List<string> filterSplitters = new List<string>();
                        filterSplitters.Add(" or ");
                        filterSplitters.Add(" and ");
                        filterSplitters.Add("%20or%20");
                        filterSplitters.Add("%20and%20");
                        filterSplitters.Add(" or ".ToUpper());
                        filterSplitters.Add(" and ".ToUpper());
                        filterSplitters.Add("%20or%20".ToUpper());
                        filterSplitters.Add("%20and%20".ToUpper());

                        string[] filters = value.Split(filterSplitters.ToArray(), StringSplitOptions.RemoveEmptyEntries);
                        List<ODataFilter> ParsedFilters = new List<ODataFilter>();
                        foreach (string f in filters)
                        {

                            //Splitters to separate out the components. This is designed to split the 3 parts (column name, operator, value)
                            List<string> SplitterToComponents = new List<string>();
                            SplitterToComponents.Add(" eq ");
                            SplitterToComponents.Add(" gt ");
                            SplitterToComponents.Add(" lt ");
                            SplitterToComponents.Add(" ne ");
                            SplitterToComponents.Add(" ge ");
                            SplitterToComponents.Add(" le ");
                            SplitterToComponents.Add("%20eq%20");
                            SplitterToComponents.Add("%20gt%20");
                            SplitterToComponents.Add("%20lt%20");
                            SplitterToComponents.Add("%20ne%20");
                            SplitterToComponents.Add("%20ge%20");
                            SplitterToComponents.Add("%20le%20");
                            SplitterToComponents.Add(" eq ".ToUpper());
                            SplitterToComponents.Add(" gt ".ToUpper());
                            SplitterToComponents.Add(" lt ".ToUpper());
                            SplitterToComponents.Add(" ne ".ToUpper());
                            SplitterToComponents.Add(" ge ".ToUpper());
                            SplitterToComponents.Add(" le ".ToUpper());
                            SplitterToComponents.Add("%20eq%20".ToUpper());
                            SplitterToComponents.Add("%20gt%20".ToUpper());
                            SplitterToComponents.Add("%20lt%20".ToUpper());
                            SplitterToComponents.Add("%20ne%20".ToUpper());
                            SplitterToComponents.Add("%20ge%20".ToUpper());
                            SplitterToComponents.Add("%20le%20".ToUpper());
                            
                            //Split it
                            string[] filterParts = f.Split(SplitterToComponents.ToArray(), StringSplitOptions.RemoveEmptyEntries);  //The first string will be the column name, the second will be the value. The comparison operator will be removed because it was split out.

                            //Determine which one was the one that split it
                            int StartLocationOfValue = f.IndexOf(filterParts[1]);
                            string ComparisonOperatorString = f.Substring(filterParts[0].Length, StartLocationOfValue - filterParts[0].Length);
                            ComparisonOperatorString = ComparisonOperatorString.ToLower().Replace("%20", "").Replace(" ", "");

                            //Construct the filter
                            ODataFilter ThisFilter = new ODataFilter();
                            ThisFilter.ColumnName = filterParts[0];
                            ThisFilter.Operator = ToReturn.StringToOperator(ComparisonOperatorString);
                            ThisFilter.SetValue(filterParts[1].Replace("%20", " "));

                            //Is there a logical operator attached to this?
                            List<string> OrTests = new List<string>();
                            OrTests.Add("or " + f);
                            OrTests.Add("or%20" + f);
                            OrTests.Add("OR " + f);
                            OrTests.Add("OR%20" + f);
                            List<string> AndTests = new List<string>();
                            AndTests.Add("and " + f);
                            AndTests.Add("and%20" + f);
                            AndTests.Add("AND " + f);
                            AndTests.Add("AND%20" + f);
                            foreach (string OrTest in OrTests)
                            {
                                if (value.Contains(OrTest))
                                {
                                    ThisFilter.LogicalOperatorPrefix = LogicalOperator.Or;
                                }
                            }
                            foreach (string AndTest in AndTests)
                            {
                                if (value.Contains(AndTest))
                                {
                                    ThisFilter.LogicalOperatorPrefix = LogicalOperator.And;
                                }
                            }

                            ParsedFilters.Add(ThisFilter);
                        }
                        ToReturn.filter.Clear();
                        ToReturn.filter.AddRange(ParsedFilters);
                    }

                    //orderby
                    if (key.ToLower() == "$orderby")
                    {
                        string[] OrderByOrders = value.Split(new string[]{","}, StringSplitOptions.RemoveEmptyEntries);
                        List<ODataOrder> orders = new List<ODataOrder>();
                        foreach (string orderStr in OrderByOrders)
                        {
                            string[] OrderParts = orderStr.Split(new string[]{" ", "%20"}, StringSplitOptions.RemoveEmptyEntries);
                            if (OrderParts.Length == 2)
                            {
                                ODataOrder order = new ODataOrder();
                                order.ColumnName = OrderParts[0];
                                if (OrderParts[1].ToLower() == "asc")
                                {
                                    order.Direction = OrderDirection.Ascending;
                                }
                                else if (OrderParts[1].ToLower() == "desc")
                                {
                                    order.Direction = OrderDirection.Descending;
                                }
                                else
                                {
                                    throw new Exception("OrderBy direction '" + orderStr + "' not recognized as valid order direction.");
                                }
                                orders.Add(order);
                            }
                        }
                        ToReturn.orderby.Clear();
                        ToReturn.orderby.AddRange(orders);
                    }

                    //top
                    if (key.ToLower() == "$top")
                    {
                        try
                        {
                            ToReturn.top = Convert.ToInt32(value);
                        }
                        catch
                        {
                            throw new Exception("Value '" + value + "' is not a valid integer, used as the top parameter in the query");
                        }
                    }

                    //skip
                    if (key.ToLower() == "$skip")
                    {
                        try
                        {
                            ToReturn.skip = Convert.ToInt32(value);
                        }
                        catch
                        {
                            throw new Exception("Value '" + value + "' is not a valid integer, used as the skip parameter in the query");
                        }
                    }

                    //count
                    if(key.ToLower() == "$count")
                    {
                        if (value.ToLower() == "true")
                        {
                            ToReturn.count = true;
                        }
                        else if (value == "1")
                        {
                            ToReturn.count = true;
                        }
                        else if (value.ToLower() == "false")
                        {
                            ToReturn.count = false;
                        }
                        else if (value == "0")
                        {
                            ToReturn.count = false;
                        }
                        else
                        {
                            throw new Exception("Value '" + value + "' not valid for parameter 'count'");
                        }
                    }
                }
          
            }

            return ToReturn;
        }

        public static ODataOperation Parse(HttpRequestMessage request)
        {

            //Create starting point
            ODataOperation ToReturn;
            if (request.RequestUri != null)
            {
                ToReturn = Parse(request.RequestUri); //Does the query params and stuff
            }
            else
            {
                ToReturn = new ODataOperation();
            }
            

            //Operation
            if (request.Method == HttpMethod.Get)
            {
                ToReturn.Operation = DataOperation.Read;
            }
            else if (request.Method == HttpMethod.Post)
            {
                ToReturn.Operation = DataOperation.Create;
            }
            else if (request.Method == new HttpMethod("PATCH") || request.Method == HttpMethod.Put)
            {
                ToReturn.Operation = DataOperation.Update;
            }
            else if (request.Method == HttpMethod.Delete)
            {
                ToReturn.Operation = DataOperation.Delete;
            }
            else
            {
                throw new Exception("Unable to parse HttpRequestMessage into OData query - unable to determine intention of '" + request.Method.ToString() + "' method.");
            }

            //Is there a body?
            if (ToReturn.Operation == DataOperation.Create || ToReturn.Operation == DataOperation.Update)
            {
                if (request.Content != null)
                {
                    string body = request.Content.ReadAsStringAsync().Result;
                    try
                    {
                        ToReturn.Payload = JObject.Parse(body);
                    }
                    catch
                    {
                        throw new Exception("Unable to parse HttpRequestMessage into ODataOperation: The body of the request message was not valid JSON.");
                    }
                }
            }
            

            return ToReturn;
        }

        #endregion


        public string ToSql(string pk_name)
        {
            if (Operation == DataOperation.Read)
            {

                DownstreamHelper dh = new DownstreamHelper();

                //If it is a count, do that
                if (count == true)
                {
                    dh.Columns.Add("count(*)");
                }

                
                dh.Table = Resource;

                //Top
                if (top.HasValue)
                {
                    dh.Top = top;
                }

                //Add each field
                foreach (string s in select)
                {
                    dh.Columns.Add(s);
                }

                //Add the where clause - record identifier
                if (RecordIdentifier != null)
                {
                    if (RecordIdentifier != "")
                    {
                        dh.Where.Add(new ConditionalClause(pk_name, Sql.ComparisonOperator.Equals, RecordIdentifier, true));
                    }
                }

                
                //Add the where clauses
                foreach (ODataFilter filter in filter)
                {
                    ConditionalClause cc = new ConditionalClause();
                    cc.ColumnName = filter.ColumnName;
                    cc.Operator = ODataOperatorToSqlOperator(filter.Operator);
                    cc.Value = filter.Value;
                    cc.UseQuotes = false; //a string in the url would already have those quotes, well at least single quotes
                    dh.Where.Add(cc);
                }

                //Order by
                foreach (ODataOrder odob in orderby)
                {
                    ReadOrder ro = new ReadOrder();
                    ro.ColumnName = odob.ColumnName;
                    if (odob.Direction == OrderDirection.Ascending)
                    {
                        ro.Direction = Sql.OrderDirection.Ascending;
                    }
                    else if (odob.Direction == OrderDirection.Descending)
                    {
                        ro.Direction = Sql.OrderDirection.Descending;
                    }
                    dh.OrderBy.Add(ro);
                }

                return dh.ToString();
            }
            else if (Operation == DataOperation.Create || Operation == DataOperation.Update)
            {
                TimHanewich.Sql.UpstreamHelper uh = new UpstreamHelper(Resource);

                //Add the properties we have received
                if (Payload != null)
                {
                    foreach (JProperty prop in Payload.Properties())
                    {
                        if (prop.Value != null)
                        {
                            if (prop.Value.Type != JTokenType.Null)
                            {
                                //Does it need quotes
                                bool NeedsQuotes = false;
                                if (ValuesThatNeedQuotations().Contains(prop.Value.Type))
                                {
                                    NeedsQuotes = true;
                                }

                                //Add it!
                                uh.Add(prop.Name, prop.Value.ToString(), NeedsQuotes);
                            }
                        }
                    }
                }
                

                if (Operation == DataOperation.Create)
                {
                    return uh.ToInsert();
                }
                else if (Operation == DataOperation.Update)
                {

                    //Add the where clause - record identifier
                    if (RecordIdentifier != null)
                    {
                        if (RecordIdentifier != "")
                        {
                            uh.AddWhereClause(new ConditionalClause(pk_name, Sql.ComparisonOperator.Equals, RecordIdentifier, true));
                        }
                    }

                    //Need to add the where conditions
                    foreach (ODataFilter filter in filter)
                    {
                        ConditionalClause cc = new ConditionalClause();
                        cc.ColumnName = filter.ColumnName;
                        cc.Operator = ODataOperatorToSqlOperator(filter.Operator);
                        cc.Value = filter.Value;
                        cc.UseQuotes = false; //a string in the url would already have those quotes, well at least single quotes
                        uh.AddWhereClause(cc);
                    }

                    return uh.ToUpdate();
                }
                else //This should never happen
                {
                    throw new Exception("Create/Update operation not triggered.");
                }
            }
            else if (Operation == DataOperation.Delete)
            {
                string ToReturn = "delete from " + Resource;

                //Add the where clause - record identifier
                if (RecordIdentifier != null)
                {
                    if (RecordIdentifier != "")
                    {
                        ToReturn = ToReturn + " where " + pk_name + " = '" + RecordIdentifier + "'";
                    }
                }
                
                //Where
                if (filter.Count > 0)
                {
                    ToReturn = ToReturn + " where ";
                    foreach (ODataFilter filt in filter)
                    {
                        string lp = "";
                        if (filt.LogicalOperatorPrefix.HasValue)
                        {
                            lp = filt.LogicalOperatorPrefix.Value.ToString().ToLower() + " ";
                        }
                        ToReturn = ToReturn + lp + filt.ColumnName + " " + filt.Operator.ToSymbol() + " " + filt.Value + " ";
                    }
                }

                return ToReturn.Trim();
            }
            else
            {
                throw new Exception("This class is not able to create a SQL operation for a '" + Operation.ToString() + " OData operation.");
            }
        }

        public string ToSql()
        {
            return ToSql("<PRIMARY_KEY>");
        }

        public HttpRequestMessage ToHttpRequestMessage(string odata_endpoint)
        {
            HttpRequestMessage ToReturn = new HttpRequestMessage();

            //Method
            if (Operation == DataOperation.Create)
            {
                ToReturn.Method = HttpMethod.Post;
            }
            else if (Operation == DataOperation.Read)
            {
                ToReturn.Method = HttpMethod.Get;
            }
            else if (Operation == DataOperation.Update)
            {
                ToReturn.Method = new HttpMethod("PATCH");
            }
            else if (Operation == DataOperation.Delete)
            {
                ToReturn.Method = HttpMethod.Delete;
            }

            //URL
            UriBuilder ub = new UriBuilder(odata_endpoint);

            //Construct the query params
            NameValueCollection nvc = HttpUtility.ParseQueryString(string.Empty);

            //filter
            string filterstmt = "";
            List<ODataFilter> filtersToDoInOrder = new List<ODataFilter>(); //We need to ensure the first one is not one with an operator
            foreach (ODataFilter f in filter)
            {
                if (f.LogicalOperatorPrefix.HasValue == false)
                {
                    filtersToDoInOrder.Insert(0, f);
                }
                else
                {
                    filtersToDoInOrder.Add(f);
                }
            }
            foreach (ODataFilter f in filtersToDoInOrder)
            {
                filterstmt = filterstmt + f.ToODataString() + " ";
            }
            if (filterstmt.Length > 0)
            {
                filterstmt = filterstmt.Substring(0, filterstmt.Length-1);
                nvc.Add("$filter", filterstmt);
            }
            
            
            //select
            string selectstmt = "";
            foreach (string c in select)
            {
                selectstmt = selectstmt + c + ",";
            }
            if (selectstmt.Length > 0)
            {
                selectstmt = selectstmt.Substring(0, selectstmt.Length-1); //Remove the final trailing comma
                nvc.Add("$select", selectstmt);
            }

            //order by
            string orderbystmt = "";
            foreach (ODataOrder o in orderby)
            {
                orderbystmt = orderbystmt + o.ToODataString() + ",";
            }
            if (orderbystmt.Length > 0)
            {
                orderbystmt = orderbystmt.Substring(0, orderbystmt.Length-1); //Remove the final trailing comma
                nvc.Add("$orderby", orderbystmt);
            }

            //Count
            if (count == true)
            {
                nvc.Add("$count", "true");
            }

            //top
            if (top.HasValue)
            {
                nvc.Add("$top", top.Value.ToString());
            }

            //skip
            if (skip.HasValue)
            {
                nvc.Add("$skip", skip.Value.ToString());
            }

            //Set the uri
            ub.Scheme = "https";
            ub.Path = ub.Path + "/" + Resource;
            ub.Path = ub.Path.Replace("//", "/"); //If a double slash was accidentally made, fix it.
            if (RecordIdentifier != null)
            {
                if (RecordIdentifier != "")
                {
                    ub.Path = ub.Path + "(" + RecordIdentifier + ")";
                }
            }

            ub.Query = nvc.ToString(); //Save the query
            ToReturn.RequestUri = ub.Uri;

            //Any payload?
            if (Operation == DataOperation.Create || Operation == DataOperation.Update)
            {
                if (Payload != null)
                {
                    ToReturn.Content = new StringContent(Payload.ToString(), Encoding.UTF8, "application/json");
                }
            }


            return ToReturn;


        }

        public void Validate()
        {
            //If it is an update or delete operation, check if they are using filters. If they are, ensure it was allowed. If not, throw an error
            if (Operation == DataOperation.Update || Operation == DataOperation.Delete)
            {
                if (Settings.AllowMultiRowModification == false)
                {
                    if (filter != null)
                    {
                        if (filter.Count > 0)
                        {
                            throw new Exception("Multi-row modification is not allowed, yet filters were used");
                        }
                    }
                }
            }

            //If it is an update or delete, MAKE SURE they are specifying a single record to modify via the record identifier
            if (Operation == DataOperation.Update || Operation == DataOperation.Delete)
            {
                if (Settings.AllowMultiRowModification == false)
                {
                    if (RecordIdentifier == null || RecordIdentifier == "")
                    {
                        throw new Exception("Multi-row modification is not allowed. You must specify a single record to modify using the record modifier.");
                    }
                }
            }

        }


        #region "Utility functions"

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

        private ComparisonOperator StringToOperator(string s)
        {
            switch (s.ToLower())
            {
                case "eq":
                    return ComparisonOperator.Equals;
                case "gt":
                    return ComparisonOperator.GreaterThan;
                case "lt":
                    return ComparisonOperator.LessThan;
                case "ne":
                    return ComparisonOperator.NotEqualTo;
                case "ge":
                    return ComparisonOperator.GreaterThanOrEqualTo;
                case "le":
                    return ComparisonOperator.LessThanOrEqualTo;
                default:
                    throw new Exception("String '" + s + "' not recognized as a valid comparsion operator.");
            }
        }

        private JTokenType[] ValuesThatNeedQuotations()
        {
            List<JTokenType> ToReturn = new List<JTokenType>();
            ToReturn.Add(JTokenType.String);
            ToReturn.Add(JTokenType.Date);
            ToReturn.Add(JTokenType.TimeSpan);
            ToReturn.Add(JTokenType.Undefined);
            return ToReturn.ToArray();
        }

        private ComparisonOperator SqlOperatorToODataOperator(TimHanewich.Sql.ComparisonOperator co)
        {
            if (co == TimHanewich.Sql.ComparisonOperator.Equals)
            {
                return ComparisonOperator.Equals;
            }
            else if (co == TimHanewich.Sql.ComparisonOperator.GreaterThan)
            {
                return ComparisonOperator.GreaterThan;
            }
            else if (co == TimHanewich.Sql.ComparisonOperator.GreaterThanOrEqual)
            {
                return ComparisonOperator.GreaterThanOrEqualTo;
            }
            else if (co == TimHanewich.Sql.ComparisonOperator.LessThan)
            {
                return ComparisonOperator.LessThan;
            }
            else if (co == TimHanewich.Sql.ComparisonOperator.LessThanOrEqual)
            {
                return ComparisonOperator.LessThanOrEqualTo;
            }
            else if (co == TimHanewich.Sql.ComparisonOperator.Not)
            {
                return ComparisonOperator.NotEqualTo;
            }
            else
            {
                throw new Exception("There is not an equivalent OData comparison operator for the supplied SQL operator");
            }
        }

        private TimHanewich.Sql.ComparisonOperator ODataOperatorToSqlOperator(ComparisonOperator co)
        {
            if (co == ComparisonOperator.Equals)
            {
                return TimHanewich.Sql.ComparisonOperator.Equals;
            }
            else if (co == ComparisonOperator.GreaterThan)
            {
                return TimHanewich.Sql.ComparisonOperator.GreaterThan;
            }
            else if (co == ComparisonOperator.GreaterThanOrEqualTo)
            {
                return TimHanewich.Sql.ComparisonOperator.GreaterThanOrEqual;
            }
            else if (co == ComparisonOperator.LessThan)
            {
                return TimHanewich.Sql.ComparisonOperator.LessThan;
            }
            else if (co == ComparisonOperator.LessThanOrEqualTo)
            {
                return TimHanewich.Sql.ComparisonOperator.LessThanOrEqual;
            }
            else if (co == ComparisonOperator.NotEqualTo)
            {
                return TimHanewich.Sql.ComparisonOperator.Not;
            }
            else
            {
                throw new Exception("There is not an SQL operator equivalent to the provided OData operator");
            }
        }

        #endregion

    }
}