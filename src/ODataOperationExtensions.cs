using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TimHanewich.OData
{
    // public static class ODataOperationExtensions
    // {
    //     //Ignores the resource in the operation since only a one dimensional array of data is provided (a single table), not multiple tables
    //     public static void PerformOnCollection(this ODataOperation operation, JArray data)
    //     {
    //         if (operation.Operation == DataOperation.Create)
    //         {
    //             if (operation.Payload != null)
    //             {
    //                 data.Add(operation.Payload);
    //             }
    //         }
    //         else if (operation.Operation == DataOperation.Read)
    //         {
    //             JArray ToReturn = new JArray();
    //             foreach (JObject jo in data)
    //             {
    //                 JObject ToInclude = new JObject();

    //                 //Should this be included?
    //                 bool ShouldInclude = true;

    //                 if (operation)


    //                 if (ToInclude.Count > 0)
    //                 {
    //                     ToReturn.Add(ToInclude);
    //                 }
    //             }
    //         }

    //     }
    // }
}