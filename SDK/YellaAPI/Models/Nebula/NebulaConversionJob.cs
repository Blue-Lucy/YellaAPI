using System;
using System.Collections.Generic;

namespace YellaAPI.Models.Nebula
{
    public class NebulaConversionJob
    {
        public Guid Id { get; set; }

        public string Status { get; set; }  // Complete | Error | <other>

        public string Error { get; set; }   // Null | <error>

        public string Progress { get; set; }

        public int Percent { get; set; }

        public string Predictedendmsutc { get; set; }   // tttttt <-- check what this looks like

        public string Ref { get; set; }

        public IEnumerable<(string, string)> Params { get; set; }
    }
}
