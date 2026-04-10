using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data;
using practiceApplication.Models;
using MyFullStackProject.Models;


namespace practiceApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TreasureController : Controller
    {
        private readonly IConfiguration _configuration;

        public TreasureController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("InsertClient")]
        public IActionResult InsertClient(string FullName, string Email, string passwords)
        {
            string message = "";

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("RegisterYourSelf", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@FullName", FullName);
                cmd.Parameters.AddWithValue("@Email", Email);
                cmd.Parameters.AddWithValue("@passwords", passwords);

                con.Open();

                message = cmd.ExecuteScalar()?.ToString();
            }

            return Json(new { message = message });
        }

        [HttpGet("LoginClient")]
        public IActionResult LoginClient(string Email, string passwords)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("LoginClient", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Email", Email);
                cmd.Parameters.AddWithValue("@passwords", passwords);

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return Json(new
                    {
                        success = true,
                        ClientID = reader["ClientID"],
                        FullName = reader["FullName"].ToString(),
                        Email = reader["Email"].ToString()
                    });
                }

                return Json(new { success = false });
            }
        }

        [HttpGet("GetProducts")]
        public IActionResult GetProducts()
        {
            List<object> products = new List<object>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("GetProducts", con);
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    products.Add(new
                    {
                        Productid = reader["Productid"],
                        Productname = reader["Productname"].ToString(),
                        price = reader["price"],
                        Productdescription = reader["Productdescription"].ToString(),
                        Productquentity = reader["Productquentity"],
                        Productimage = "https://localhost:7107/images/" + reader["Productimage"].ToString()
                    });
                }
            }

            return Json(products);
        }

        [HttpPost("AddToCart")]
        public IActionResult AddToCart([FromBody] Models.CartModel cart)
        {
            int currentQuantity = 0;

            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("AddToCart", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ClientID", cart.ClientID);
                    cmd.Parameters.AddWithValue("@Productid", cart.Productid);
                    cmd.Parameters.AddWithValue("@Productimage", cart.Productimage);
                    cmd.Parameters.AddWithValue("@Quantity", cart.Quantity);
                    cmd.Parameters.AddWithValue("@Price", cart.Price);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // Get total quantity of items in the cart
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand countCmd = new SqlCommand("SELECT SUM(Quantity) FROM Cart WHERE ClientID=@ClientID", con))
                {
                    countCmd.Parameters.AddWithValue("@ClientID", cart.ClientID);
                    con.Open();
                    var result = countCmd.ExecuteScalar();
                    currentQuantity = result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }

                return Json(new { success = true, message = "Product added to cart", totalQuantity = currentQuantity });
            }
            catch (SqlException ex)
            {
                // Detect stock exception from SQL
                if (ex.Message.Contains("Not enough stock available"))
                {
                    return Json(new { success = false, message = "Cannot add more than available stock", totalQuantity = currentQuantity });
                }

                // Other SQL errors
                return Json(new { success = false, message = "Error adding product to cart: " + ex.Message, totalQuantity = currentQuantity });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message, totalQuantity = currentQuantity });
            }
        }


        [HttpGet("Cart")]
        public IActionResult Cart(int ClientID)
        {
            List<object> cart = new List<object>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("SelectCart", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ClientID", ClientID);
                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    cart.Add(new
                    {
                        Cartid = reader["Cartid"],
                        ClientID = reader["ClientID"],
                        Productid = reader["Productid"],
                        Productimage = reader["Productimage"].ToString(),
                        Productname = reader["Productname"].ToString(),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        Price = Convert.ToDecimal(reader["TotalPrice"]),
                        Productquentity = Convert.ToInt32(reader["Productquentity"]) ,
                        Productdescription = reader["Productdescription"].ToString(),
                    });
                }
            }

            return Json(cart);
        }


        [HttpGet("RemoveItem")]
        public IActionResult RemoveItem(int Cartid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    SqlCommand cmd = new SqlCommand("RemoveCartItem", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Cartid", Cartid);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "item removed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("UpdateCartQuantity")]
        public IActionResult UpdateCartQuantity(int Cartid, int Quantity)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("UpdateQuantity", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Cartid", Cartid);
                    cmd.Parameters.AddWithValue("@Quantity", Quantity);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Quantity updated successfully" });
            }
            catch (SqlException ex)
            {
                
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpGet("Contect")]
        public IActionResult Contect(string FullName, string Email, string messagees)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("messagees", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@FullName", FullName);
                cmd.Parameters.AddWithValue("@Email", Email);
                cmd.Parameters.AddWithValue("@messagees", messagees);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return Json(new { message = "messagees send Successfully" });
        }

        [HttpGet("SearchProduct")]
        public IActionResult SearchProduct(string name)
        {
            List<object> products = new List<object>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("SearchProduct", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@name", name);

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    products.Add(new
                    {
                        productid = reader["Productid"],
                        productname = reader["Productname"],
                        productdescription = reader["Productdescription"],
                        price = reader["Price"],
                        Productimage = "https://localhost:7107/images/" + reader["Productimage"].ToString(),
                        Productquentity = reader["Productquentity"]
                    });
                }

                con.Close();
            }

            return Json(products);
        }







        [HttpPost("PlaceOrder")]
        public IActionResult PlaceOrder([FromBody] PlaceOrderRequest order)
        {
            try
            {
                string message = "";

                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("PlaceOrder", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@ClientID", order.ClientID);
                        cmd.Parameters.AddWithValue("@CustomerName", order.CustomerName);
                        cmd.Parameters.AddWithValue("@CustomerEmail", order.CustomerEmail);
                        cmd.Parameters.AddWithValue("@CustomerAddress", order.CustomerAddress);
                        cmd.Parameters.AddWithValue("@PaymentMethod", order.PaymentMethod);

                        SqlParameter msgParam = new SqlParameter("@Message", SqlDbType.VarChar, 500)
                        {
                            Direction = ParameterDirection.Output
                        };

                        cmd.Parameters.Add(msgParam);

                        con.Open();
                        cmd.ExecuteNonQuery();

                        message = msgParam.Value.ToString();
                    }
                }

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("OrderHistory")]
        public IActionResult OrderHistory(int ClientID)
        {
            List<object> orders = new List<object>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("OrderHistory", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ClientID", ClientID);

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    orders.Add(new
                    {
                        OrderId = Convert.ToInt32(reader["OrderId"]),
                        Productname = reader["Productname"].ToString(),
                        Productimage = "https://localhost:7107/images/" + reader["Productimage"].ToString(),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        Price = Convert.ToDecimal(reader["Price"]),
                        OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                        Status = reader["Status"].ToString()
                    });
                }

                con.Close();
            }

            return Json(orders);
        }

        [HttpPost("ClearHistory")]
        public IActionResult ClearHistory(int ClientID)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("ClearOrderHistory", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@ClientID", ClientID);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            return Json(new { success = true, message = "History cleared successfully" });
        }

        [HttpPost("DeleteOrder")]
        public IActionResult DeleteOrder(int OrderId)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("DeleteOrderHistory", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@OrderId", OrderId);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            return Json(new { success = true, message = "Order removed from history" });
        }

        // Add to Wishlist
        [HttpPost("AddWishlist")]
        public IActionResult AddWishlist([FromBody] Wishlist w)
        {
            if (w == null)
                return BadRequest("Wishlist object is required");
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("sp_AddToWishlist", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ClientID", w.ClientID);
                    cmd.Parameters.AddWithValue("@ProductID", w.ProductID);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true, message = "Added to Wishlist" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        // Get Wishlist
        [HttpGet("GetWishlist")]
        public IActionResult GetWishlist(int ClientID)
        {
            List<object> wishlist = new List<object>();
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("sp_GetWishlist", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ClientID", ClientID);

                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        wishlist.Add(new
                        {
                            ProductID = reader["ProductID"],
                            ProductName = reader["Productname"].ToString(),
                            ProductImage = "https://localhost:7107/images/" + reader["Productimage"].ToString()
                        });
                    }
                }
                return Json(new { success = true, wishlist });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }




        [HttpPost("RemoveWishlist")]
        public IActionResult RemoveWishlist([FromBody] Wishlist w)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("sp_RemoveWishlist", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ClientID", w.ClientID);
                    cmd.Parameters.AddWithValue("@ProductID", w.ProductID);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true, message = "Removed from Wishlist" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



    
        // Add or Update Wishlist Item with Notes/Tags
        [HttpPost("AddOrUpdateWishlist")]
        public IActionResult AddOrUpdateWishlist([FromBody] Wishlist w)
        {
            if (w == null)
                return BadRequest("Wishlist object is required");
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("AddOrUpdateWishlist", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ClientID", w.ClientID);
                    cmd.Parameters.AddWithValue("@ProductID", w.ProductID);
                    cmd.Parameters.AddWithValue("@Note", (object)w.Note ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Tags", (object)w.Tags ?? DBNull.Value);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true, message = "Wishlist updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        // Get Wishlist with Notes and Tags
        [HttpGet("GetWishlistWithNotes")]
        public IActionResult GetWishlistWithNotes(int ClientID)
        {
            List<object> wishlist = new List<object>();

            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("GetWishlistWithNotes", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ClientID", ClientID);

                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        wishlist.Add(new
                        {
                            ProductID = reader["ProductID"],
                            ProductName = reader["Productname"].ToString(), // match your SQL alias
                            Productimage = reader["Productimage"].ToString(),
                            Note = reader["Note"] == DBNull.Value ? "" : reader["Note"].ToString(),
                            Tags = reader["Tags"] == DBNull.Value ? "" : reader["Tags"].ToString()
                        });
                    }
                }
                return Json(new { success = true, wishlist });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("InsertProduct")]
        public async Task<IActionResult> InsertProduct(
     [FromForm] string productname,
     [FromForm] decimal price,
     [FromForm] string productdescription,
     [FromForm] int productquentity,
     IFormFile image)
        {

            if (image == null || image.Length == 0)
                return BadRequest("Image not uploaded");

            // Correct path
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            // Create folder if not exists
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

            string filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            SqlCommand cmd = new SqlCommand("ProductInsert", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Productname", productname);
            cmd.Parameters.AddWithValue("@Price", price);
            cmd.Parameters.AddWithValue("@Productdescription", productdescription);
            cmd.Parameters.AddWithValue("@productquentity", productquentity);
            cmd.Parameters.AddWithValue("@productimage", fileName);

            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();

            return Ok(new { message = "Product Added Successfully" });

        }

       

    }
}
