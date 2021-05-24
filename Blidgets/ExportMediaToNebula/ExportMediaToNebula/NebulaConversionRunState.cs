using System;
using System.Collections.Generic;

namespace ExportMediaToNebula
{
    public class NebulaConversionRunState
    {
        public IList<NebulaConversionJob> Jobs { get; set; } = new List<NebulaConversionJob>();
    }

    public class NebulaConversionJob
    {
        public Guid JobId { get; set; }

        public int AssetFileId { get; set; }

        public string OriginalFilename { get; set; }

        public bool Complete { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        public int Percent { get; set; }
    }
}
