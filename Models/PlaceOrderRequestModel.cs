namespace MyFullStackProject.Models
{
    public class PlaceOrderRequest
    {
        public int ClientID { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerAddress { get; set; }
        public string PaymentMethod { get; set; }
    }
}
