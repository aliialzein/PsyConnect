namespace PsyConnect.Models
{
    public class StripeSettings
    {
        public string PublishableKey { get; set; }
        public string SecretKey { get; set; }

        // Optional: you can hardcode these in code instead
        public string SuccessUrlBase { get; set; }
        public string CancelUrlBase { get; set; }
    }
}
