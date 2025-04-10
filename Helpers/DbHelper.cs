using System.Data;
using System.Data.SqlClient;

namespace ADLRestaurant.Helpers
{
    public static class DbHelper
    {
        // Connection string ni once set cheyyadam kosam
        public static string ConnectionString { get; set; }

        // Init method tho connection string assign cheyyali startup lo
        public static void Init(string connectionString)
        {
            ConnectionString = connectionString;
        }

        // SELECT query ki use chestham - returns SqlDataReader
        public static SqlDataReader ExecuteReader(string spName, Dictionary<string, object> parameters)
        {
            SqlConnection con = new SqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(spName, con)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, parameters); // Parameters ni add cheyyadam
            con.Open();

            // Reader return cheyyadam, connection close avvadam ki CloseConnection flag
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        // INSERT / UPDATE / DELETE ki use cheyyadam - returns number of rows affected
        public static int ExecuteNonQuery(string spName, Dictionary<string, object> parameters)
        {
            using SqlConnection con = new SqlConnection(ConnectionString);
            using SqlCommand cmd = new SqlCommand(spName, con)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, parameters);
            con.Open();
            return cmd.ExecuteNonQuery(); // returns int (affected rows)
        }

        // Single value return avvali ante (like count, new id) - use this
        public static object ExecuteScalar(string spName, Dictionary<string, object> parameters)
        {
            using SqlConnection con = new SqlConnection(ConnectionString);
            using SqlCommand cmd = new SqlCommand(spName, con)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, parameters);
            con.Open();
            return cmd.ExecuteScalar(); // returns object (convert as needed)
        }

        // Common method to add parameters from Dictionary to SqlCommand
        private static void AddParameters(SqlCommand cmd, Dictionary<string, object> parameters)
        {
            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    // Null values ni handle cheyyadam kosam DBNull.Value use cheysthunna
                    cmd.Parameters.AddWithValue(pair.Key, pair.Value ?? DBNull.Value);
                }
            }
        }
    }
}
