using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace NanNingWebServiceSpace
{
    //OCI功能集合
    class OracleUtils
    {
        private OracleConnection m_OrclConnect;

        public OracleCommand m_OrclCommand;

        public bool ConnectToOracle()
        {
            string strDataSource = ConfigurationManager.AppSettings.Get("Oracle_DataSource");
            string strUser = ConfigurationManager.AppSettings.Get("Oracle_User");
            string strPassword = ConfigurationManager.AppSettings.Get("Oracle_PassWord");
            string strSqlConnection = string.Format("Data Source={0};User ID={1};Password={2}",
                 strDataSource, strUser, strPassword);
            try
            {
                if (m_OrclConnect == null)
                {
                    m_OrclConnect = new OracleConnection(strSqlConnection);
                    m_OrclConnect.Open();
                    m_OrclCommand = new OracleCommand();
                    m_OrclCommand.Connection = m_OrclConnect;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ReleaseResource()
        {
            if (m_OrclConnect != null)
            {
                m_OrclConnect.Close();
                m_OrclConnect.Dispose();
            }
            if (m_OrclCommand != null)
            {
                m_OrclCommand.Dispose();
            }
        }

        //取一个表中一行的一个字段
        public object GetRowSingleField(string strSql)
        {
            m_OrclCommand.CommandText = strSql;

            OracleDataReader Oracle_Reader = m_OrclCommand.ExecuteReader();

            object strValue = null;
            if (Oracle_Reader.HasRows)
            {
                Oracle_Reader.Read();

                strValue = Oracle_Reader.GetValue(0);
            }

            Oracle_Reader.Close();
            Oracle_Reader.Dispose();

            return strValue;
        }

        //取一个表中一行的多个字段
        public List<object> GetRowMultipField(string strSql)
        {
            m_OrclCommand.CommandText = strSql;

            OracleDataReader Oracle_Reader = m_OrclCommand.ExecuteReader();

            //根据这里的限制条件，只能查询到一条记录（对应一个新增业务）
            List<object> strValues = new List<object>();
            if (Oracle_Reader.HasRows)
            {
                Oracle_Reader.Read();
                for (int i = 0; i < Oracle_Reader.FieldCount; i++)
                {
                    object strValue = Oracle_Reader.GetValue(i);
                    strValues.Add(strValue);
                }
            }

            Oracle_Reader.Close();
            Oracle_Reader.Dispose();

            return strValues;
        }
    }
}
