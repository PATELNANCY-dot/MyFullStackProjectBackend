namespace MyFullStackProject.Models
{
    public class UpdateProductModel
    {
        public int ProductID { get; set; }

        public string Productname { get; set; }

        public decimal Price { get; set; }

        public string Productdescription { get; set; }

        // IMPORTANT: nullable = optional file
        public IFormFile? Image { get; set; }

        public int Productquentity { get; set; }
    }
}