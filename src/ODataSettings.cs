using System;

namespace TimHanewich.OData
{
    public class ODataSettings
    {
        public bool AllowMultiRowModification {get; set;}

        public ODataSettings()
        {
            AllowMultiRowModification = false;
        }
    }
}