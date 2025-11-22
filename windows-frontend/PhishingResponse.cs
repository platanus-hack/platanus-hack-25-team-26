using System;

namespace PhishingFinder_v2
{
    public class PhishingResponse
    {
        public int Scoring { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}

