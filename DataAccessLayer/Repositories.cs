// ============================================================
// SmartCafe Pro - Data Access Layer
// ============================================================
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using SmartCafePro.Models;
using SmartCafePro.Interfaces;

namespace SmartCafePro.DataAccessLayer
{
    // ==========================================================
    // SINGLETON PATTERN: Database Connection Manager
    // ==========================================================
    public sealed class DatabaseManager : IDatabaseConnection
    {
        private static DatabaseManager _instance;
        private static readonly object _lock = new();
        private string _connectionString;

        // Private constructor — prevents external instantiation
        private DatabaseManager()
        {
            _connectionString = System.Configuration.ConfigurationManager
                .ConnectionStrings["SmartCafeDB"]?.ConnectionString
                ?? "Server=localhost;Database=SmartCafeProDB;Integrated Security=true;TrustServerCertificate=true;";
        }

        public static DatabaseManager Instance
        {
            get
            {
                if (_instance == null)
                    lock (_lock)
                        if (_instance == null)
                            _instance = new DatabaseManager();
                return _instance;
            }
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public bool TestConnection()
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void UpdateConnectionString(string server, string database,
                                           string user = null, string pass = null)
        {
            _connectionString = string.IsNullOrEmpty(user)
                ? $"Server={server};Database={database};Integrated Security=true;TrustServerCertificate=true;"
                : $"Server={server};Database={database};User Id={user};Password={pass};TrustServerCertificate=true;";
        }

        public string ConnectionString => _connectionString;
    }

    // ==========================================================
    // REPOSITORY PATTERN: Base Repository
    // ==========================================================
    public abstract class BaseRepository<T> where T : BaseModel
    {
        protected SqlConnection GetConn() => DatabaseManager.Instance.GetConnection();

        protected SqlCommand BuildCmd(SqlConnection conn, string sql,
                                       CommandType type = CommandType.StoredProcedure)
        {
            var cmd = new SqlCommand(sql, conn) { CommandType = type };
            return cmd;
        }

        protected void AddParam(SqlCommand cmd, string name, object value)
            => cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);

        protected T? SafeGet<T>(IDataReader r, string col)
        {
            var idx = r.GetOrdinal(col);
            return r.IsDBNull(idx) ? default : (T)r.GetValue(idx);
        }

        // Template Method pattern: subclasses define mapping
        protected abstract T MapFromReader(IDataReader r);
    }

    // ==========================================================
    // User Repository
    // ==========================================================
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public User GetByID(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                "SELECT u.*, r.RoleName FROM Users u INNER JOIN Roles r ON u.RoleID=r.RoleID WHERE u.UserID=@id",
                conn) { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapFromReader(r) : null;
        }

        public User GetByUsername(string username)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                "SELECT u.*, r.RoleName FROM Users u INNER JOIN Roles r ON u.RoleID=r.RoleID WHERE u.Username=@u",
                conn) { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@u", username);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapFromReader(r) : null;
        }

        public List<User> GetAll()
        {
            var list = new List<User>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                "SELECT u.*, r.RoleName FROM Users u INNER JOIN Roles r ON u.RoleID=r.RoleID ORDER BY u.Username",
                conn) { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<User> GetActive()
        {
            var list = new List<User>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                "SELECT u.*, r.RoleName FROM Users u INNER JOIN Roles r ON u.RoleID=r.RoleID WHERE u.IsActive=1 ORDER BY u.Username",
                conn) { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public bool Insert(User user)
        {
            try
            {
                using var conn = GetConn(); conn.Open();
                using var cmd = new SqlCommand(
                    "INSERT INTO Users(Username,PasswordHash,PasswordSalt,RoleID,Email) VALUES(@u,@ph,@ps,@rid,@em)",
                    conn) { CommandType = CommandType.Text };
                cmd.Parameters.AddWithValue("@u",   user.Username);
                cmd.Parameters.AddWithValue("@ph",  user.PasswordHash);
                cmd.Parameters.AddWithValue("@ps",  user.PasswordSalt);
                cmd.Parameters.AddWithValue("@rid", user.RoleID);
                cmd.Parameters.AddWithValue("@em",  (object)user.Email ?? DBNull.Value);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch { return false; }
        }

        public bool Update(User user)
        {
            try
            {
                using var conn = GetConn(); conn.Open();
                using var cmd = new SqlCommand(
                    "UPDATE Users SET RoleID=@rid,Email=@em,IsActive=@ia,UpdatedAt=GETDATE() WHERE UserID=@id",
                    conn) { CommandType = CommandType.Text };
                cmd.Parameters.AddWithValue("@rid", user.RoleID);
                cmd.Parameters.AddWithValue("@em",  (object)user.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ia",  user.IsActive);
                cmd.Parameters.AddWithValue("@id",  user.UserID);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch { return false; }
        }

        public bool Delete(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand("UPDATE Users SET IsActive=0 WHERE UserID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdatePassword(int userID, string newHash, string salt)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE Users SET PasswordHash=@ph,PasswordSalt=@ps,UpdatedAt=GETDATE() WHERE UserID=@id",
                conn) { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@ph", newHash);
            cmd.Parameters.AddWithValue("@ps", salt);
            cmd.Parameters.AddWithValue("@id", userID);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateLastLogin(int userID)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand("UPDATE Users SET LastLogin=GETDATE() WHERE UserID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", userID);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateRememberToken(int userID, string token)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand("UPDATE Users SET RememberToken=@t WHERE UserID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@t",  (object)token ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", userID);
            return cmd.ExecuteNonQuery() > 0;
        }

        protected override User MapFromReader(IDataReader r) => new()
        {
            UserID       = (int)r["UserID"],
            Username     = r["Username"].ToString(),
            PasswordHash = r["PasswordHash"].ToString(),
            PasswordSalt = r["PasswordSalt"].ToString(),
            RoleID       = (int)r["RoleID"],
            RoleName     = r["RoleName"].ToString(),
            Email        = r["Email"] as string,
            IsActive     = (bool)r["IsActive"],
            LastLogin    = r["LastLogin"] as DateTime?
        };
    }

    // ==========================================================
    // Customer Repository
    // ==========================================================
    public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
    {
        private const string BASE_QUERY =
            "SELECT * FROM Customers";

        public string LastError { get; private set; } = "";

        public Customer GetByID(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(BASE_QUERY + " WHERE CustomerID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapFromReader(r) : null;
        }

        public Customer GetByPhone(string phone)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(BASE_QUERY + " WHERE Phone=@p", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@p", phone);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapFromReader(r) : null;
        }

        public List<Customer> GetAll()
        {
            var list = new List<Customer>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(BASE_QUERY + " ORDER BY FirstName", conn)
                { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<Customer> GetActive()
        {
            var list = new List<Customer>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(BASE_QUERY + " WHERE IsActive=1 ORDER BY FirstName", conn)
                { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<Customer> Search(string keyword)
        {
            var list = new List<Customer>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                BASE_QUERY + " WHERE FirstName LIKE @kw OR LastName LIKE @kw OR Phone LIKE @kw OR Email LIKE @kw",
                conn) { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public bool Insert(Customer c)
        {
            LastError = "";
            try
            {
                using var conn = GetConn(); conn.Open();
                using var cmd = new SqlCommand("sp_UpsertCustomer", conn)
                    { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@CustomerID",  DBNull.Value);
                cmd.Parameters.AddWithValue("@FirstName",   c.FirstName);
                cmd.Parameters.AddWithValue("@LastName",    c.LastName);
                cmd.Parameters.AddWithValue("@Phone",       DbString(c.Phone));
                cmd.Parameters.AddWithValue("@Email",       DbString(c.Email));
                cmd.Parameters.AddWithValue("@Address",     DbString(c.Address));
                cmd.Parameters.AddWithValue("@DateOfBirth", (object)c.DateOfBirth ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Notes",       DbString(c.Notes));
                var outParam = new SqlParameter("@NewID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outParam);
                cmd.ExecuteNonQuery();
                c.CustomerID = (int)outParam.Value;
                return c.CustomerID > 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return InsertDirect(c);
            }
        }

        public bool Update(Customer c)
        {
            LastError = "";
            try
            {
                using var conn = GetConn(); conn.Open();
                using var cmd = new SqlCommand("sp_UpsertCustomer", conn)
                    { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@CustomerID",  c.CustomerID);
                cmd.Parameters.AddWithValue("@FirstName",   c.FirstName);
                cmd.Parameters.AddWithValue("@LastName",    c.LastName);
                cmd.Parameters.AddWithValue("@Phone",       DbString(c.Phone));
                cmd.Parameters.AddWithValue("@Email",       DbString(c.Email));
                cmd.Parameters.AddWithValue("@Address",     DbString(c.Address));
                cmd.Parameters.AddWithValue("@DateOfBirth", (object)c.DateOfBirth ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Notes",       DbString(c.Notes));
                var outParam = new SqlParameter("@NewID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outParam);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return UpdateDirect(c);
            }
        }

        private bool InsertDirect(Customer c)
        {
            try
            {
                using var conn = GetConn();
                conn.Open();
                EnsureCustomerSchema(conn);

                using var cmd = new SqlCommand(@"
INSERT INTO Customers (FirstName, LastName, Phone, Email, Address, DateOfBirth,
                       MembershipLevel, TotalSpent, LoyaltyPoints, Notes, IsActive)
VALUES (@FirstName, @LastName, @Phone, @Email, @Address, @DateOfBirth,
        'Bronze', 0, 0, @Notes, 1);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn)
                { CommandType = CommandType.Text };

                AddCustomerParams(cmd, c);
                c.CustomerID = Convert.ToInt32(cmd.ExecuteScalar());
                return c.CustomerID > 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        private bool UpdateDirect(Customer c)
        {
            try
            {
                using var conn = GetConn();
                conn.Open();
                EnsureCustomerSchema(conn);

                using var cmd = new SqlCommand(@"
UPDATE Customers
   SET FirstName=@FirstName,
       LastName=@LastName,
       Phone=@Phone,
       Email=@Email,
       Address=@Address,
       DateOfBirth=@DateOfBirth,
       Notes=@Notes,
       UpdatedAt=GETDATE()
 WHERE CustomerID=@CustomerID;", conn)
                { CommandType = CommandType.Text };

                AddCustomerParams(cmd, c);
                cmd.Parameters.AddWithValue("@CustomerID", c.CustomerID);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        private static void AddCustomerParams(SqlCommand cmd, Customer c)
        {
            cmd.Parameters.AddWithValue("@FirstName", c.FirstName?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@LastName", c.LastName?.Trim() ?? "");
            cmd.Parameters.AddWithValue("@Phone", DbString(c.Phone));
            cmd.Parameters.AddWithValue("@Email", DbString(c.Email));
            cmd.Parameters.AddWithValue("@Address", DbString(c.Address));
            cmd.Parameters.AddWithValue("@DateOfBirth", (object)c.DateOfBirth ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", DbString(c.Notes));
        }

        private static void EnsureCustomerSchema(SqlConnection conn)
        {
            using var cmd = new SqlCommand(@"
IF COL_LENGTH('dbo.Customers', 'Address') IS NULL
    ALTER TABLE dbo.Customers ADD Address NVARCHAR(300) NULL;
IF COL_LENGTH('dbo.Customers', 'DateOfBirth') IS NULL
    ALTER TABLE dbo.Customers ADD DateOfBirth DATE NULL;
IF COL_LENGTH('dbo.Customers', 'MembershipLevel') IS NULL
    ALTER TABLE dbo.Customers ADD MembershipLevel NVARCHAR(20) NOT NULL CONSTRAINT DF_Customers_MembershipLevel DEFAULT 'Bronze';
IF COL_LENGTH('dbo.Customers', 'TotalSpent') IS NULL
    ALTER TABLE dbo.Customers ADD TotalSpent DECIMAL(12,2) NOT NULL CONSTRAINT DF_Customers_TotalSpent DEFAULT 0;
IF COL_LENGTH('dbo.Customers', 'LoyaltyPoints') IS NULL
    ALTER TABLE dbo.Customers ADD LoyaltyPoints INT NOT NULL CONSTRAINT DF_Customers_LoyaltyPoints DEFAULT 0;
IF COL_LENGTH('dbo.Customers', 'Notes') IS NULL
    ALTER TABLE dbo.Customers ADD Notes NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.Customers', 'IsActive') IS NULL
    ALTER TABLE dbo.Customers ADD IsActive BIT NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT 1;
IF COL_LENGTH('dbo.Customers', 'JoinDate') IS NULL
    ALTER TABLE dbo.Customers ADD JoinDate DATETIME2 NOT NULL CONSTRAINT DF_Customers_JoinDate DEFAULT GETDATE();
IF COL_LENGTH('dbo.Customers', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.Customers ADD UpdatedAt DATETIME2 NULL;

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = @sql + N'ALTER TABLE dbo.Customers DROP CONSTRAINT ' + QUOTENAME(x.name) + N';'
FROM (
    SELECT DISTINCT kc.name
    FROM sys.key_constraints kc
    JOIN sys.index_columns ic ON kc.parent_object_id = ic.object_id
                             AND kc.unique_index_id = ic.index_id
    JOIN sys.columns c ON ic.object_id = c.object_id
                      AND ic.column_id = c.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Customers')
      AND kc.type = 'UQ'
      AND c.name IN (N'Phone', N'Email')
) x;

SELECT @sql = @sql + N'DROP INDEX ' + QUOTENAME(x.name) + N' ON dbo.Customers;'
FROM (
    SELECT DISTINCT i.name
    FROM sys.indexes i
    JOIN sys.index_columns ic ON i.object_id = ic.object_id
                             AND i.index_id = ic.index_id
    JOIN sys.columns c ON ic.object_id = c.object_id
                      AND ic.column_id = c.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.Customers')
      AND i.is_unique = 1
      AND i.is_primary_key = 0
      AND i.is_unique_constraint = 0
      AND c.name IN (N'Phone', N'Email')
) x;

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;", conn)
            { CommandType = CommandType.Text };
            cmd.ExecuteNonQuery();
        }

        private static object DbString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        public bool Delete(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand("UPDATE Customers SET IsActive=0 WHERE CustomerID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateLoyaltyPoints(int customerID, int points)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE Customers SET LoyaltyPoints=LoyaltyPoints+@pts WHERE CustomerID=@id",
                conn) { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@pts", points);
            cmd.Parameters.AddWithValue("@id",  customerID);
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<Order> GetOrderHistory(int customerID)
        {
            var list = new List<Order>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand("sp_CustomerHistory", conn)
                { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@CustomerID", customerID);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Order
                {
                    OrderID     = (int)r["OrderID"],
                    OrderNumber = r["OrderNumber"].ToString(),
                    OrderDate   = (DateTime)r["OrderDate"],
                    OrderType   = r["OrderType"].ToString(),
                    TotalAmount = (decimal)r["TotalAmount"],
                    Status      = r["Status"].ToString()
                });
            }
            return list;
        }

        protected override Customer MapFromReader(IDataReader r) => new()
        {
            CustomerID      = (int)r["CustomerID"],
            FirstName       = r["FirstName"].ToString(),
            LastName        = r["LastName"].ToString(),
            Phone           = r["Phone"] as string,
            Email           = r["Email"] as string,
            Address         = r["Address"] as string,
            DateOfBirth     = r["DateOfBirth"] as DateTime?,
            MembershipLevel = r["MembershipLevel"].ToString(),
            TotalSpent      = (decimal)r["TotalSpent"],
            LoyaltyPoints   = (int)r["LoyaltyPoints"],
            Notes           = r["Notes"] as string,
            IsActive        = (bool)r["IsActive"],
            JoinDate        = (DateTime)r["JoinDate"]
        };
    }

    // ==========================================================
    // Product Repository
    // ==========================================================
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        private const string BASE_QUERY =
            "SELECT p.*, c.CategoryName FROM Products p INNER JOIN Categories c ON p.CategoryID=c.CategoryID";

        public Product GetByID(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(BASE_QUERY + " WHERE p.ProductID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapFromReader(r) : null;
        }

        public List<Product> GetAll()
        {
            var list = new List<Product>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(BASE_QUERY + " ORDER BY c.DisplayOrder, p.DisplayOrder", conn)
                { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<Product> GetActive()
        {
            var list = new List<Product>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(BASE_QUERY + " WHERE p.IsActive=1 ORDER BY c.DisplayOrder, p.DisplayOrder", conn)
                { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<Product> GetAvailable()
        {
            var list = new List<Product>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(BASE_QUERY + " WHERE p.IsActive=1 AND p.IsAvailable=1 ORDER BY c.DisplayOrder, p.DisplayOrder", conn)
                { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<Product> GetByCategory(int categoryID)
        {
            var list = new List<Product>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(BASE_QUERY + " WHERE p.CategoryID=@cid AND p.IsActive=1 ORDER BY p.DisplayOrder", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@cid", categoryID);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<Product> Search(string keyword)
        {
            var list = new List<Product>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(BASE_QUERY + " WHERE p.ProductName LIKE @kw OR p.Description LIKE @kw", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public Product GetByBarcode(string barcode)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(BASE_QUERY + " WHERE p.Barcode=@b", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@b", barcode);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapFromReader(r) : null;
        }

        public bool Insert(Product p)
        {
            try
            {
                using var conn = GetConn(); conn.Open();
                using var cmd = new SqlCommand(
                    @"INSERT INTO Products(CategoryID,ProductName,Description,Price,CostPrice,
                      ImagePath,IsCombo,IsAvailable,PrepTimeMin,Calories,Allergens,Barcode,DisplayOrder)
                      VALUES(@cid,@pn,@desc,@pr,@cp,@ip,@ic,@ia,@ptm,@cal,@all,@bc,@do)",
                    conn) { CommandType = CommandType.Text };
                cmd.Parameters.AddWithValue("@cid",  p.CategoryID);
                cmd.Parameters.AddWithValue("@pn",   p.ProductName);
                cmd.Parameters.AddWithValue("@desc", (object)p.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@pr",   p.Price);
                cmd.Parameters.AddWithValue("@cp",   p.CostPrice);
                cmd.Parameters.AddWithValue("@ip",   (object)p.ImagePath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ic",   p.IsCombo);
                cmd.Parameters.AddWithValue("@ia",   p.IsAvailable);
                cmd.Parameters.AddWithValue("@ptm",  p.PrepTimeMin);
                cmd.Parameters.AddWithValue("@cal",  (object)p.Calories ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@all",  (object)p.Allergens ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@bc",   (object)p.Barcode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@do",   p.DisplayOrder);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch { return false; }
        }

        public bool Update(Product p)
        {
            try
            {
                using var conn = GetConn(); conn.Open();
                using var cmd = new SqlCommand(
                    @"UPDATE Products SET CategoryID=@cid,ProductName=@pn,Description=@desc,
                      Price=@pr,CostPrice=@cp,ImagePath=@ip,IsCombo=@ic,IsAvailable=@ia,
                      PrepTimeMin=@ptm,Calories=@cal,Allergens=@all,Barcode=@bc,
                      DisplayOrder=@do,UpdatedAt=GETDATE() WHERE ProductID=@id",
                    conn) { CommandType = CommandType.Text };
                cmd.Parameters.AddWithValue("@id",   p.ProductID);
                cmd.Parameters.AddWithValue("@cid",  p.CategoryID);
                cmd.Parameters.AddWithValue("@pn",   p.ProductName);
                cmd.Parameters.AddWithValue("@desc", (object)p.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@pr",   p.Price);
                cmd.Parameters.AddWithValue("@cp",   p.CostPrice);
                cmd.Parameters.AddWithValue("@ip",   (object)p.ImagePath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ic",   p.IsCombo);
                cmd.Parameters.AddWithValue("@ia",   p.IsAvailable);
                cmd.Parameters.AddWithValue("@ptm",  p.PrepTimeMin);
                cmd.Parameters.AddWithValue("@cal",  (object)p.Calories ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@all",  (object)p.Allergens ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@bc",   (object)p.Barcode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@do",   p.DisplayOrder);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch { return false; }
        }

        public bool Delete(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand("UPDATE Products SET IsActive=0 WHERE ProductID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool ToggleAvailability(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand("UPDATE Products SET IsAvailable=~IsAvailable WHERE ProductID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        protected override Product MapFromReader(IDataReader r) => new()
        {
            ProductID    = (int)r["ProductID"],
            CategoryID   = (int)r["CategoryID"],
            CategoryName = r["CategoryName"].ToString(),
            ProductName  = r["ProductName"].ToString(),
            Description  = r["Description"] as string,
            Price        = (decimal)r["Price"],
            CostPrice    = (decimal)r["CostPrice"],
            ImagePath    = r["ImagePath"] as string,
            IsCombo      = (bool)r["IsCombo"],
            IsAvailable  = (bool)r["IsAvailable"],
            PrepTimeMin  = (int)r["PrepTimeMin"],
            Calories     = r["Calories"] as int?,
            Allergens    = r["Allergens"] as string,
            Barcode      = r["Barcode"] as string,
            DisplayOrder = (int)r["DisplayOrder"],
            IsActive     = (bool)r["IsActive"]
        };
    }

    // ==========================================================
    // Order Repository
    // ==========================================================
    public class OrderRepository : BaseRepository<Order>, IOrderRepository
    {
        public Order GetByID(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                @"SELECT o.*,
                    ISNULL(c.FirstName+' '+c.LastName,'Walk-in') AS CustomerName,
                    t.TableNumber,
                    e.FirstName+' '+e.LastName AS EmployeeName
                  FROM Orders o
                  LEFT JOIN Customers c  ON o.CustomerID=c.CustomerID
                  LEFT JOIN CafeTables t ON o.TableID=t.TableID
                  LEFT JOIN Employees e  ON o.EmployeeID=e.EmployeeID
                  WHERE o.OrderID=@id", conn) { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapFromReader(r) : null;
        }

        public Order GetWithDetails(int id)
        {
            var order = GetByID(id);
            if (order == null) return null;

            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                @"SELECT od.*,p.ProductName
                  FROM OrderDetails od
                  INNER JOIN Products p ON od.ProductID=p.ProductID
                  WHERE od.OrderID=@id ORDER BY od.OrderDetailID", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                order.Items.Add(new OrderDetail
                {
                    OrderDetailID = (int)r["OrderDetailID"],
                    OrderID       = id,
                    ProductID     = (int)r["ProductID"],
                    ProductName   = r["ProductName"].ToString(),
                    Quantity      = (int)r["Quantity"],
                    UnitPrice     = (decimal)r["UnitPrice"],
                    Discount      = (decimal)r["Discount"],
                    KitchenStatus = r["KitchenStatus"].ToString(),
                    Notes         = r["Notes"] as string
                });
            }
            return order;
        }

        public List<Order> GetAll()
        {
            var list = new List<Order>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                @"SELECT TOP 500 o.*,
                    ISNULL(c.FirstName+' '+c.LastName,'Walk-in') AS CustomerName,
                    t.TableNumber,
                    e.FirstName+' '+e.LastName AS EmployeeName
                  FROM Orders o
                  LEFT JOIN Customers c  ON o.CustomerID=c.CustomerID
                  LEFT JOIN CafeTables t ON o.TableID=t.TableID
                  LEFT JOIN Employees e  ON o.EmployeeID=e.EmployeeID
                  ORDER BY o.OrderDate DESC", conn) { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<Order> GetActive()
        {
            var list = new List<Order>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                @"SELECT o.*,
                    ISNULL(c.FirstName+' '+c.LastName,'Walk-in') AS CustomerName,
                    t.TableNumber,
                    e.FirstName+' '+e.LastName AS EmployeeName
                  FROM Orders o
                  LEFT JOIN Customers c  ON o.CustomerID=c.CustomerID
                  LEFT JOIN CafeTables t ON o.TableID=t.TableID
                  LEFT JOIN Employees e  ON o.EmployeeID=e.EmployeeID
                  WHERE o.Status NOT IN ('Completed','Cancelled')
                  ORDER BY o.OrderDate DESC", conn) { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<Order> GetActiveOrders()
        {
            var list = new List<Order>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                @"SELECT o.*, ISNULL(c.FirstName+' '+c.LastName,'Walk-in') AS CustomerName,
                    t.TableNumber, e.FirstName+' '+e.LastName AS EmployeeName
                  FROM Orders o
                  LEFT JOIN Customers c  ON o.CustomerID=c.CustomerID
                  LEFT JOIN CafeTables t ON o.TableID=t.TableID
                  LEFT JOIN Employees e  ON o.EmployeeID=e.EmployeeID
                  WHERE o.Status NOT IN ('Completed','Cancelled')
                  ORDER BY o.OrderDate", conn) { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<Order> GetByStatus(string status)
        {
            var list = new List<Order>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                @"SELECT o.*, ISNULL(c.FirstName+' '+c.LastName,'Walk-in') AS CustomerName,
                    t.TableNumber, e.FirstName+' '+e.LastName AS EmployeeName
                  FROM Orders o
                  LEFT JOIN Customers c  ON o.CustomerID=c.CustomerID
                  LEFT JOIN CafeTables t ON o.TableID=t.TableID
                  LEFT JOIN Employees e  ON o.EmployeeID=e.EmployeeID
                  WHERE o.Status=@s ORDER BY o.OrderDate DESC", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@s", status);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<Order> GetByDate(DateTime date)
        {
            var list = new List<Order>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                @"SELECT o.*, ISNULL(c.FirstName+' '+c.LastName,'Walk-in') AS CustomerName,
                    t.TableNumber, e.FirstName+' '+e.LastName AS EmployeeName
                  FROM Orders o
                  LEFT JOIN Customers c  ON o.CustomerID=c.CustomerID
                  LEFT JOIN CafeTables t ON o.TableID=t.TableID
                  LEFT JOIN Employees e  ON o.EmployeeID=e.EmployeeID
                  WHERE CAST(o.OrderDate AS DATE)=@d ORDER BY o.OrderDate DESC", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@d", date.Date);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<Order> GetByDateRange(DateTime start, DateTime end)
        {
            var list = new List<Order>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                "SELECT * FROM fn_GetOrdersByDateRange(@s,@e) ORDER BY OrderDate DESC", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@s", start.Date);
            cmd.Parameters.AddWithValue("@e", end.Date);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Order
                {
                    OrderID      = (int)r["OrderID"],
                    OrderNumber  = r["OrderNumber"].ToString(),
                    OrderDate    = (DateTime)r["OrderDate"],
                    OrderType    = r["OrderType"].ToString(),
                    Status       = r["Status"].ToString(),
                    TotalAmount  = (decimal)r["TotalAmount"],
                    CustomerName = r["CustomerName"].ToString(),
                    EmployeeName = r["CashierName"] as string,
                    TableNumber  = r["TableNumber"] as string
                });
            return list;
        }

        public List<Order> GetByCustomer(int customerID)
        {
            var list = new List<Order>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                @"SELECT o.*, ISNULL(c.FirstName+' '+c.LastName,'Walk-in') AS CustomerName,
                    t.TableNumber, e.FirstName+' '+e.LastName AS EmployeeName
                  FROM Orders o
                  LEFT JOIN Customers c  ON o.CustomerID=c.CustomerID
                  LEFT JOIN CafeTables t ON o.TableID=t.TableID
                  LEFT JOIN Employees e  ON o.EmployeeID=e.EmployeeID
                  WHERE o.CustomerID=@cid ORDER BY o.OrderDate DESC", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@cid", customerID);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public bool Insert(Order o)
        {
            // Handled via sp_PlaceOrder — use OrderService instead
            return false;
        }

        public bool Update(Order o)
        {
            try
            {
                using var conn = GetConn(); conn.Open();
                using var cmd = new SqlCommand(
                    "UPDATE Orders SET Status=@s,Notes=@n,UpdatedAt=GETDATE() WHERE OrderID=@id", conn)
                    { CommandType = CommandType.Text };
                cmd.Parameters.AddWithValue("@s",  o.Status);
                cmd.Parameters.AddWithValue("@n",  (object)o.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", o.OrderID);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch { return false; }
        }

        public bool Delete(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand("UPDATE Orders SET Status='Cancelled' WHERE OrderID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateStatus(int orderID, string status)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE Orders SET Status=@s,UpdatedAt=GETDATE() WHERE OrderID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@s",  status);
            cmd.Parameters.AddWithValue("@id", orderID);
            return cmd.ExecuteNonQuery() > 0;
        }

        public string GenerateOrderNumber()
            => $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        protected override Order MapFromReader(IDataReader r) => new()
        {
            OrderID      = (int)r["OrderID"],
            OrderNumber  = r["OrderNumber"].ToString(),
            CustomerID   = r["CustomerID"] as int?,
            CustomerName = r["CustomerName"]?.ToString(),
            TableID      = r["TableID"] as int?,
            TableNumber  = r["TableNumber"] as string,
            EmployeeID   = r["EmployeeID"] as int?,
            EmployeeName = r["EmployeeName"] as string,
            OrderType    = r["OrderType"].ToString(),
            Status       = r["Status"].ToString(),
            SubTotal     = (decimal)r["SubTotal"],
            DiscountAmt  = (decimal)r["DiscountAmt"],
            TaxAmt       = (decimal)r["TaxAmt"],
            TotalAmount  = (decimal)r["TotalAmount"],
            Notes        = r["Notes"] as string,
            OrderDate    = (DateTime)r["OrderDate"],
            CompletedAt  = r["CompletedAt"] as DateTime?
        };
    }

    // ==========================================================
    // Notification Repository
    // ==========================================================
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        public Notification GetByID(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand("SELECT * FROM Notifications WHERE NotificationID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapFromReader(r) : null;
        }

        public List<Notification> GetAll()
        {
            var list = new List<Notification>();
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                "SELECT TOP 100 * FROM Notifications ORDER BY CreatedAt DESC", conn)
                { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<Notification> GetActive() => GetAll();

        public List<Notification> GetUnread(string role = null)
        {
            var list = new List<Notification>();
            using var conn = GetConn(); conn.Open();
            var sql = "SELECT TOP 50 * FROM Notifications WHERE IsRead=0";
            if (!string.IsNullOrEmpty(role)) sql += " AND (TargetRole IS NULL OR TargetRole=@r)";
            sql += " AND (ExpiresAt IS NULL OR ExpiresAt > GETDATE()) ORDER BY CreatedAt DESC";
            using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text };
            if (!string.IsNullOrEmpty(role)) cmd.Parameters.AddWithValue("@r", role);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public int GetUnreadCount(string role = null)
        {
            using var conn = GetConn(); conn.Open();
            var sql = "SELECT COUNT(*) FROM Notifications WHERE IsRead=0";
            if (!string.IsNullOrEmpty(role)) sql += " AND (TargetRole IS NULL OR TargetRole=@r)";
            using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text };
            if (!string.IsNullOrEmpty(role)) cmd.Parameters.AddWithValue("@r", role);
            return (int)cmd.ExecuteScalar();
        }

        public bool MarkAsRead(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand("UPDATE Notifications SET IsRead=1 WHERE NotificationID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool MarkAllRead(string role = null)
        {
            using var conn = GetConn(); conn.Open();
            var sql = "UPDATE Notifications SET IsRead=1 WHERE IsRead=0";
            if (!string.IsNullOrEmpty(role)) sql += " AND (TargetRole IS NULL OR TargetRole=@r)";
            using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text };
            if (!string.IsNullOrEmpty(role)) cmd.Parameters.AddWithValue("@r", role);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Insert(Notification n)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                "INSERT INTO Notifications(Title,Message,Type,TargetRole,ExpiresAt) VALUES(@t,@m,@ty,@tr,@ex)", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@t",  n.Title);
            cmd.Parameters.AddWithValue("@m",  n.Message);
            cmd.Parameters.AddWithValue("@ty", n.Type);
            cmd.Parameters.AddWithValue("@tr", (object)n.TargetRole ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ex", (object)n.ExpiresAt ?? DBNull.Value);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Update(Notification n) => MarkAsRead(n.NotificationID);
        public bool Delete(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand("DELETE FROM Notifications WHERE NotificationID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        protected override Notification MapFromReader(IDataReader r) => new()
        {
            NotificationID = (int)r["NotificationID"],
            Title          = r["Title"].ToString(),
            Message        = r["Message"].ToString(),
            Type           = r["Type"].ToString(),
            TargetRole     = r["TargetRole"] as string,
            IsRead         = (bool)r["IsRead"],
            CreatedAt      = (DateTime)r["CreatedAt"],
            ExpiresAt      = r["ExpiresAt"] as DateTime?
        };
    }
}
