using Digital_Pharma.Models;

namespace Digital_Pharma.Models
{
    // هذا الكلاس الجديد يمثل "نتيجة التحليل" التي ستظهر للمستخدم
    public class InteractionAnalysis
    {
        public OpenFdaResult Drug1 { get; set; } = new();
        public OpenFdaResult Drug2 { get; set; } = new();

        // هل تم العثور على تداخل؟
        public bool InteractionFound { get; set; }

        // نص يوضح أين وجدنا التداخل
        public string AnalysisMessage { get; set; } = string.Empty;

        // درجة الخطورة (اختياري للمستقبل)
        public string Severity { get; set; } = "Unknown";
    }
}