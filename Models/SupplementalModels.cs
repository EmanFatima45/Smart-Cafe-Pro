// ============================================================
// SmartCafe Pro - Category Model (missing from Models.cs)
// ============================================================
namespace SmartCafePro.Models
{
    public class Category : BaseModel
    {
        public int    CategoryID   { get; set; }
        public string CategoryName { get; set; }
        public string Description  { get; set; }
        public string IconPath     { get; set; }
        public int    DisplayOrder { get; set; }

        public override bool Validate() =>
            !string.IsNullOrWhiteSpace(CategoryName);

        public override string GetDisplayName() => CategoryName;
    }
}

// ============================================================
// SmartCafe Pro - TableRepository (missing from DAL)
// ============================================================
namespace SmartCafePro.DataAccessLayer
{
    using System.Collections.Generic;
    using System.Data;
    using Microsoft.Data.SqlClient;
    using SmartCafePro.Models;
    using SmartCafePro.Interfaces;

    public class TableRepository : BaseRepository<CafeTable>, ITableRepository
    {
        private const string BASE_SQL =
            "SELECT * FROM CafeTables WHERE IsActive=1";

        public CafeTable GetByID(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd  = new SqlCommand(BASE_SQL + " AND TableID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapFromReader(r) : null;
        }

        public List<CafeTable> GetAll()
        {
            var list = new List<CafeTable>();
            using var conn = GetConn(); conn.Open();
            using var cmd  = new SqlCommand(BASE_SQL + " ORDER BY TableNumber", conn)
                { CommandType = CommandType.Text };
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public List<CafeTable> GetActive() => GetAll();

        public List<CafeTable> GetByStatus(string status)
        {
            var list = new List<CafeTable>();
            using var conn = GetConn(); conn.Open();
            using var cmd  = new SqlCommand(BASE_SQL + " AND Status=@s ORDER BY TableNumber", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@s", status);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapFromReader(r));
            return list;
        }

        public bool Insert(CafeTable t)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                "INSERT INTO CafeTables(TableNumber,Capacity,Location) VALUES(@n,@c,@l)", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@n", t.TableNumber);
            cmd.Parameters.AddWithValue("@c", t.Capacity);
            cmd.Parameters.AddWithValue("@l", (object)t.Location ?? System.DBNull.Value);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Update(CafeTable t)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE CafeTables SET TableNumber=@n,Capacity=@c,Location=@l,Status=@s WHERE TableID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@n",  t.TableNumber);
            cmd.Parameters.AddWithValue("@c",  t.Capacity);
            cmd.Parameters.AddWithValue("@l",  (object)t.Location ?? System.DBNull.Value);
            cmd.Parameters.AddWithValue("@s",  t.Status);
            cmd.Parameters.AddWithValue("@id", t.TableID);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Delete(int id)
        {
            using var conn = GetConn(); conn.Open();
            using var cmd = new SqlCommand("UPDATE CafeTables SET IsActive=0 WHERE TableID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateStatus(int tableID, string status)
        {
            using var conn = GetConn(); conn.Open();
            using (var schema = new SqlCommand(@"
IF COL_LENGTH('dbo.CafeTables', 'StatusChangedAt') IS NULL
    ALTER TABLE dbo.CafeTables ADD StatusChangedAt DATETIME2 NULL;", conn)
                { CommandType = CommandType.Text })
            {
                schema.ExecuteNonQuery();
            }
            using var cmd = new SqlCommand("UPDATE CafeTables SET Status=@s, StatusChangedAt=GETDATE() WHERE TableID=@id", conn)
                { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@s",  status);
            cmd.Parameters.AddWithValue("@id", tableID);
            return cmd.ExecuteNonQuery() > 0;
        }

        protected override CafeTable MapFromReader(System.Data.IDataReader r) => new()
        {
            TableID     = (int)r["TableID"],
            TableNumber = r["TableNumber"].ToString(),
            Capacity    = (int)r["Capacity"],
            Location    = r["Location"] as string,
            Status      = r["Status"].ToString(),
            QRCode      = r["QRCode"] as string,
            IsActive    = (bool)r["IsActive"]
        };
    }

    // ── Updated ServiceContainer with TableRepo ──────────────
    // Add this to ServiceContainer.Initialize() in Services.cs:
    // public static ITableRepository TableRepo => _tableRepo;
    // private static TableRepository _tableRepo;
    // _tableRepo = new TableRepository();  (in Initialize)
}
