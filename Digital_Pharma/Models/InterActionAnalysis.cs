using Digital_Pharma.Models;

namespace Digital_Pharma.Models
{

    public class InteractionAnalysis
    {
        public OpenFdaResult Drug1 { get; set; } = new();
        public OpenFdaResult Drug2 { get; set; } = new();


        public bool InteractionFound { get; set; }
    
        public string AnalysisMessage { get; set; } = string.Empty;

        
        public string Severity { get; set; } = "Unknown";
    }
}
