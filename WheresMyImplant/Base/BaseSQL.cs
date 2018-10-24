using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace WheresMyImplant
{
    internal abstract class BaseSQL : Base
    {
        protected string connectionString;

        ////////////////////////////////////////////////////////////////////////////////
        internal BaseSQL(string server, string database, string username, string password)
        {
            if (null == username)
            {
                connectionString = "Server=" + server + "; Database=" + database + "; Integrated Security=SSPI; Connection Timeout=1";
            }
            else if (username.Contains("\\"))
            {
                connectionString = "Server=" + server + "; Database=" + database + "; Integrated Security=SSPI; uid=" + username + "; pwd=" + password + "; Connection Timeout=1";
            }
            else
            {
                connectionString = "Server=" + server + "; Database=" + database + "; User ID=" + username + "; Password=" + password + "; Connection Timeout=1";
            }

            Console.WriteLine("[*] Connection String: " + connectionString);
        }

        ////////////////////////////////////////////////////////////////////////////////
        protected string ExecuteQuery(string command)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                SqlCommand sqlCommand = new SqlCommand();
                sqlCommand.CommandText = command;
                sqlCommand.CommandType = CommandType.Text;
                sqlCommand.Connection = sqlConnection;
                SqlDataReader reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        stringBuilder.Append(reader.GetString(0)+"\n");
                    }
                }
            }
            return stringBuilder.ToString();
        }
    }
}