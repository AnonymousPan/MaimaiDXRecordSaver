using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using log4net;

namespace MaimaiDXRecordSaver
{
    public class DataRecorderDB : DataRecorderBase
    {
        private SqlConnection connection;
        private SqlCommand cmdGetLastRecordID;

        private static ILog logger = LogManager.GetLogger("DataRecorderDB");

        public bool UseWinodwsAuth { get; set; }
        public string Server { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public override int GetLastRecordID()
        {
            throw new NotImplementedException();
        }

        public override MusicRecord GetMusicRecord(int id)
        {
            throw new NotImplementedException();
        }

        public override bool Init()
        {
            try
            {
                connection = new SqlConnection(GetConnectString());
                cmdGetLastRecordID = connection.CreateCommand();
                cmdGetLastRecordID.CommandText = "SELECT TOP(1) [recordID] FROM [MusicRecord] ORDER BY [recordID] DESC";
            }
            catch(Exception err)
            {

            }
            return true;
        }

        public override bool IsRecordExists(int id)
        {
            throw new NotImplementedException();
        }

        public override int SaveMusicRecord(MusicRecord rec)
        {
            throw new NotImplementedException();
        }

        private string GetConnectString()
        {
            if(UseWinodwsAuth)
            {
                return string.Format("Server={0};Database={1};Integrated Security=True",
                    Server, Database);
            }
            else
            {
                return string.Format("Server={0};Database={1};User Id={2};Password={3}",
                    Server, Database, Username, Password);
            }
        }
    }
}
