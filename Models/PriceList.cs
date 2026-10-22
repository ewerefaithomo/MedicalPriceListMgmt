namespace MedicalPriceListApp.Models
{
    public class PriceList
    {
        public string BANK_CODE { get; set; } = string.Empty;

        public string SERVICES { get; set; } = string.Empty;

        public decimal CURRENT_PRICE { get; set; }

        public string REVENUE_GENERATING_FUND { get; set; } = string.Empty;

        public string DEPARTMENT { get; set; } = string.Empty;
    }
}