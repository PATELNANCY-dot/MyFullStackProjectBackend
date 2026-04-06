using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace practiceApplication.Controllers
{
    public class TreasureController : Controller
    {
        private readonly IConfiguration _configuration;

        public TreasureController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
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

        [HttpGet]
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

        // GET ALL PRODUCTS
        [HttpGet]
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
        [HttpPost]
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


        [HttpGet]
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
                        Productquentity = Convert.ToInt32(reader["Productquentity"])  // ADD
                    });
                }
            }

            return Json(cart);
        }


        [HttpGet]
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

        [HttpGet]
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
                // Catch stock errors specifically
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpGet]
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

        [HttpGet]
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
                    });
                }

                con.Close();
            }

            return Json(products);
        }







        [HttpPost]
        public IActionResult PlaceOrder(int ClientID)
        {
            try
            {
                string message = "";

                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("PlaceOrder", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Input parameter
                        cmd.Parameters.AddWithValue("@ClientID", ClientID);

                        // Output parameter to get message from SQL
                        SqlParameter msgParam = new SqlParameter("@Message", SqlDbType.VarChar, 500)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(msgParam);

                        con.Open();
                        cmd.ExecuteNonQuery();

                        // Get the message from SQL
                        message = msgParam.Value.ToString();
                    }
                }

                // Return message to frontend
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
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

        [HttpPost]
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

        [HttpPost]
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
    }

    }


