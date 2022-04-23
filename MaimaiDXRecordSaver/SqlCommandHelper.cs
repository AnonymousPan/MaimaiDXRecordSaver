using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Data.SqlClient;

namespace MaimaiDXRecordSaver
{
    public class SqlCommandHelper
    {
        private Dictionary<string, string> commands = new Dictionary<string, string>();
        private Dictionary<string, SqlCommand> cmdObjs = new Dictionary<string, SqlCommand>();
        private SqlConnection connection;

        public SqlCommandHelper(SqlConnection conn, string commandFile)
        {
            connection = conn;
            using(StringReader reader = new StringReader(commandFile))
            {
                string str = reader.ReadLineNotEmpty();
                while(str != null)
                {
                    string cmd = reader.ReadLineNotEmpty();
                    commands.Add(str, cmd);
                    SqlCommand cmdObj = connection.CreateCommand();
                    cmdObj.CommandText = cmd;
                    cmdObj.Prepare();
                    cmdObjs.Add(str, cmdObj);
                    str = reader.ReadLineNotEmpty();
                }
            }
            if(!commands.ContainsKey("TableExists"))
            {
                throw new Exception("Necessary command not found!");
            }
        }

        public string GetCommand(string name)
        {
            string cmd;
            if(commands.TryGetValue(name, out cmd))
            {
                return cmd;
            }
            else
            {
                return null;
            }
        }

        public SqlCommand GetCommandObject(string name)
        {
            SqlCommand cmd;
            if(cmdObjs.TryGetValue(name, out cmd))
            {
                return cmd;
            }
            else
            {
                return null;
            }
        }

        public SqlCommand InitCommand(string name)
        {
            string cmd = GetCommand(name);
            if(cmd == null)
            {
                return null;
            }
            else
            {
                SqlCommand cmdObj = connection.CreateCommand();
                cmdObj.CommandText = cmd;
                return cmdObj;
            }
        }

        public int ExecuteNonQuery(string name, Dictionary<string, object> param)
        {
            SqlCommand cmd = GetCommandObject(name);
            if (cmd == null)
                throw new ArgumentException(string.Format("Command with name \"{0}\" not found!", name));
            if(param != null)
            {
                foreach (KeyValuePair<string, object> kv in param)
                {
                    cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                }
            }
            int result = cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            return result;
        }

        public int ExecuteNonQueryT(string name, ValueTuple<string, object>[] param)
        {
            SqlCommand cmd = GetCommandObject(name);
            if (cmd == null)
                throw new ArgumentException(string.Format("Command with name \"{0}\" not found!", name));
            if (param != null)
            {
                foreach (ValueTuple<string, object> t in param)
                {
                    cmd.Parameters.AddWithValue(t.Item1, t.Item2);
                }
            }
            int result = cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            return result;
        }

        public int ExecuteNonQuery(string name)
        {
            return ExecuteNonQuery(name, null);
        }

        public object ExecuteScalar(string name, Dictionary<string, object> param)
        {
            SqlCommand cmd = GetCommandObject(name);
            if (cmd == null)
                throw new ArgumentException(string.Format("Command with name \"{0}\" not found!", name));
            if(param != null)
            {
                foreach (KeyValuePair<string, object> kv in param)
                {
                    cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                }
            }
            object result = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return result;
        }

        public object ExecuteScalarT(string name, ValueTuple<string, object>[] param)
        {
            SqlCommand cmd = GetCommandObject(name);
            if (cmd == null)
                throw new ArgumentException(string.Format("Command with name \"{0}\" not found!", name));
            if (param != null)
            {
                foreach (ValueTuple<string, object> t in param)
                {
                    cmd.Parameters.AddWithValue(t.Item1, t.Item2);
                }
            }
            object result = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return result;
        }

        public object ExecuteScalar(string name)
        {
            return ExecuteScalar(name, null);
        }

        public SqlDataReader ExecuteReader(string name, Dictionary<string, object> param)
        {
            SqlCommand cmd = GetCommandObject(name);
            if (cmd == null)
                throw new ArgumentException(string.Format("Command with name \"{0}\" not found!", name));
            if(param != null)
            {
                foreach (KeyValuePair<string, object> kv in param)
                {
                    cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                }
            }
            SqlDataReader result = cmd.ExecuteReader();
            cmd.Parameters.Clear();
            return result;
        }

        public SqlDataReader ExecuteReaderT(string name, ValueTuple<string, object>[] param)
        {
            SqlCommand cmd = GetCommandObject(name);
            if (cmd == null)
                throw new ArgumentException(string.Format("Command with name \"{0}\" not found!", name));
            if (param != null)
            {
                foreach (ValueTuple<string, object> t in param)
                {
                    cmd.Parameters.AddWithValue(t.Item1, t.Item2);
                }
            }
            SqlDataReader result = cmd.ExecuteReader();
            cmd.Parameters.Clear();
            return result;
        }

        public SqlDataReader ExecuteReader(string name)
        {
            return ExecuteReader(name, null);
        }

        public bool IsTableExists(string tableName)
        {
            return !(ExecuteScalarT("TableExists", new ValueTuple<string, object>[] { ("Name", tableName) }) is DBNull);
        }
    }

    public static class StringReaderExt
    {
        public static string ReadLineNotEmpty(this StringReader reader)
        {
            string str = reader.ReadLine();
            while (str != null)
            {
                string s = str.Trim();
                if (!string.IsNullOrEmpty(s) && !s.StartsWith("#"))
                {
                    return str;
                }
                else
                {
                    str = reader.ReadLine();
                }
            }
            return null;
        }
    }
}
