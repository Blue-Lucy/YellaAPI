using System;
using System.Collections.Generic;

namespace YellaAPI.Models.Nebula
{
    public class NebulaConversionGetStatusResponse
    {
        public Dictionary<Guid, NebulaConversionJob> Ids { get; set; }
    }
}
