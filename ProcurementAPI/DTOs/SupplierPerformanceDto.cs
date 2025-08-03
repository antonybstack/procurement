using System.ComponentModel.DataAnnotations;

namespace ProcurementAPI.DTOs
{
    public class SupplierPerformanceDataDto
    {
        public int TotalQuotes { get; set; }
        public int AwardedQuotes { get; set; }
        public decimal? AvgQuotePrice { get; set; }
        public int TotalPurchaseOrders { get; set; }
    }

    public class SupplierDetailDto : SupplierDto
    {
        public SupplierPerformanceDataDto? Performance { get; set; }
    }
}
