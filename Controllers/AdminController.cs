using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyFullStackProject.Models;
using System.Data;

namespace practiceApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ADMIN LOGIN
        [HttpGet("Login")]
        public IActionResult Login(string Email, string Password)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("AdminLogin", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Email", Email);
                cmd.Parameters.AddWithValue("@Password", Password);

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return Json(new
                    {
                        success = true,
                        AdminID = reader["AdminID"],
                        Email = reader["Email"].ToString()
                    });
                }

                return Json(new { success = false });
            }
        }



        [HttpGet("GetUsers")]
        public IActionResult GetUsers()
        {
            List<object> users = new List<object>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("GetAllUsers", con);
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new
                    {
                        ClientID = reader["ClientID"],
                        FullName = reader["FullName"].ToString(),
                        Email = reader["Email"].ToString()
                    });
                }
            }

            return Json(users);
        }

        [HttpGet("GetProductById/{id}")]
        public IActionResult GetProductById(int id)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("GetProductByID", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@ProductID", id);

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var product = new
                    {
                        productID = reader["ProductID"],
                        productname = reader["Productname"].ToString(),
                        price = reader["Price"],
                        productdescription = reader["Productdescription"].ToString(),
                        productimage = reader["Productimage"].ToString(),
                        Productquentity = reader["Productquentity"]
                    };

                    return Ok(product);
                }

                return NotFound();
            }
        }


        private string GetExistingImage(int productId)
        {
            string image = "";

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT Productimage FROM Product WHERE ProductID = @id", con);
                cmd.Parameters.AddWithValue("@id", productId);

                con.Open();
                var result = cmd.ExecuteScalar();

                if (result != null)
                {
                    image = result.ToString();
                }
            }

            return image;
        }


        [HttpPost("UpdateProduct")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProduct([FromForm] UpdateProductModel model)
        {
            string fileName = null;

            // STEP 1: HANDLE IMAGE ONLY IF PROVIDED
            if (model.Image != null && model.Image.Length > 0)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Image.FileName);

                string path = Path.Combine(folder, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await model.Image.CopyToAsync(stream);
                }
            }
            else
            {
                // IMPORTANT:
                // Keep existing image from DB instead of overwriting with empty string
                fileName = GetExistingImage(model.ProductID);
            }

            // STEP 2: UPDATE DATABASE
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("ProductUpdate", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@ProductID", model.ProductID);
                cmd.Parameters.AddWithValue("@Productname", model.Productname);
                cmd.Parameters.AddWithValue("@Price", model.Price);
                cmd.Parameters.AddWithValue("@Productdescription", model.Productdescription);
                cmd.Parameters.AddWithValue("@Productimage", fileName);
                cmd.Parameters.AddWithValue("@Productquentity", model.Productquentity);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            return Ok(new { message = "Product Updated Successfully" });
        }


        [HttpGet("GetOrdersWithItems")]
        public IActionResult GetOrdersWithItems()
        {
            List<OrderWithItemsModel> orders = new List<OrderWithItemsModel>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("GetOrdersWithItemsAndUser", con);
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    orders.Add(new OrderWithItemsModel
                    {
                        OrderId = Convert.ToInt32(reader["OrderId"]),
                        ClientID = Convert.ToInt32(reader["ClientID"]),
                       
                        OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        Status = reader["Status"].ToString(),

                        OrderItemId = Convert.ToInt32(reader["OrderItemId"]),
                        ProductId = Convert.ToInt32(reader["ProductId"]),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        Price = Convert.ToDecimal(reader["Price"]),
                        LineTotal = Convert.ToDecimal(reader["LineTotal"]),

                        CustomerName = reader["CustomerName"].ToString(),
                        CustomerEmail = reader["CustomerEmail"].ToString(),
                        CustomerAddress = reader["CustomerAddress"].ToString(),
                        PaymentMethod = reader["PaymentMethod"].ToString(),

                    });
                }
            }

            return Ok(orders);
        }


        [HttpGet("GetAllUsers")]
        public IActionResult GetAllUsers()
        {
            List<object> users = new List<object>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("GetAllUsers", con);
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new
                    {
                        ClientID = Convert.ToInt32(reader["ClientID"]),
                        FullName = reader["FullName"].ToString(),
                        Email = reader["Email"].ToString()
                    });
                }
            }

            return Ok(users);
        }

        [HttpGet("GetOrderItemsWithProductDetails/{orderId}")]
        public IActionResult GetOrderItemsWithProductDetails(int orderId)
        {
            List<object> orderItems = new List<object>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("GetOrderItemsWithProductDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@OrderId", orderId);

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    orderItems.Add(new
                    {
                        OrderId = Convert.ToInt32(reader["OrderId"]),
                        ClientID = Convert.ToInt32(reader["ClientID"]),
                        OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        Status = reader["Status"].ToString(),

                        OrderItemId = Convert.ToInt32(reader["OrderItemId"]),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        Price = Convert.ToDecimal(reader["Price"]),

                        ProductId = Convert.ToInt32(reader["ProductId"]),
                        ProductName = reader["ProductName"].ToString(),
                        ProductImage = reader["ProductImage"].ToString()
                    });
                }
            }

            return Ok(orderItems);
        }



        [HttpGet("GetTopSellingProducts")]
        public IActionResult GetTopSellingProducts()
        {
            List<object> products = new List<object>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("GetTopSellingProducts", con);
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    products.Add(new
                    {
                        ProductId = Convert.ToInt32(reader["ProductId"]),
                        ProductName = reader["ProductName"].ToString(),
                        ProductImage = reader["ProductImage"].ToString(),
                        TotalSold = Convert.ToInt32(reader["TotalSold"])
                    });
                }
            }

            return Ok(products);
        }


        [HttpDelete("DeleteProduct/{id}")]
        public IActionResult DeleteProduct(int id)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("DeleteProduct", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@ProductId", id);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }

            return Ok(new { message = "Product Deleted Successfully" });
        }

    }
}  