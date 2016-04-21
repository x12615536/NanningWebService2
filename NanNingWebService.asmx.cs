using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Configuration;

using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;

namespace NanNingWebServiceSpace
{
    /// <summary>
    /// Service1 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
    // [System.Web.Script.Services.ScriptService]
    public class NanNingWebService : System.Web.Services.WebService
    {
        private OracleUtils m_OracleUtils = new OracleUtils();
        private SDEUtils m_SDEUtils = new SDEUtils();

        private string g_strDJQTableName = WebConfigurationManager.AppSettings.Get("DJQTableName");
        private string g_strDJZQTableName = WebConfigurationManager.AppSettings.Get("DZJQTableName");

        [WebMethod(Description = "输入地籍子区代码,获取一个新宗地序列号")]
        public string GetNewZDXH(string strDJZQDM)
        {
            try
            {
                //连接数据库
                if (!m_OracleUtils.ConnectToOracle())
                {
                    throw new Exception("Oracle连接失败");
                }

                string strSequenceName = "DJZQSEQUENCE_" + strDJZQDM;

                //判断：如果当前用户输入已经创建了Oracle序列，则取到当前序列+1值，如果没有则创建当前Oracle序列
                string strSQL = "SELECT count(*) FROM All_Sequences where Sequence_name='" + strSequenceName + "'";

                if(m_OracleUtils.GetRowSingleField(strSQL).ToString() == "0" )
                {
                    strSQL = "CREATE SEQUENCE " + strSequenceName + " INCREMENT BY 1 START WITH 1 MAXVALUE 99999 NOCYCLE";
                    m_OracleUtils.m_OrclCommand.CommandText = strSQL;
                    m_OracleUtils.m_OrclCommand.ExecuteNonQuery();
                }

                //获取最大宗地序列号
                strSQL = "SELECT " + strSequenceName + ".nextval FROM DUAL";
                string strXH = m_OracleUtils.GetRowSingleField(strSQL).ToString();

                return strXH;
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                m_OracleUtils.ReleaseResource();
            }
        }

        [WebMethod(Description = "输入地籍子区代码，与2位宗地特征码，获取一个19位新宗地序列号")]
        public string GetNewZDXHFull(string strDJZQDM,string strTZM1,string strTZM2)
        {
            try
            {
                //连接数据库
                if (!m_OracleUtils.ConnectToOracle())
                {
                    throw new Exception("Oracle连接失败");
                }

                string strSequenceName = "DJZQSEQUENCE_" + strDJZQDM;

                //判断：如果当前用户输入已经创建了Oracle序列，则取到当前序列+1值，如果没有则创建当前Oracle序列
                string strSQL = "SELECT count(*) FROM All_Sequences where Sequence_name='" + strSequenceName + "'";

                if (m_OracleUtils.GetRowSingleField(strSQL).ToString() == "0")
                {
                    strSQL = "CREATE SEQUENCE " + strSequenceName + " INCREMENT BY 1 START WITH 1 MAXVALUE 99999 NOCYCLE";
                    m_OracleUtils.m_OrclCommand.CommandText = strSQL;
                    m_OracleUtils.m_OrclCommand.ExecuteNonQuery();
                }

                //获取最大宗地序列号
                strSQL = "SELECT " + strSequenceName + ".nextval FROM DUAL";

                string strXH = m_OracleUtils.GetRowSingleField(strSQL).ToString();

                string strFullXH = strDJZQDM + strTZM1 + strTZM2 + strXH.PadLeft(5, '0');

                return strFullXH;
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                m_OracleUtils.ReleaseResource();
            }
        }

        [WebMethod(Description = "输入一个面地物坐标串，获取一个新的地籍子区代码")]
        public string GetNewDJZQDM(string strPointXArray,string strPointYArray)
        {
            try
            {
                //连接数据库
                if (!m_SDEUtils.ConnectToSDE())
                {
                    throw new Exception("SDE连接失败");
                }

                string[] PointXArray = strPointXArray.Split(',');
                string[] PointYArray = strPointYArray.Split(',');

                int PointCount = PointXArray.Length;

                if (!((PointXArray[0].Equals(PointXArray[PointCount - 1]))&&(PointYArray[0].Equals(PointYArray[PointCount - 1]))))
                {
                    return "输入的不是一个闭合的面地物";
                }

                //构建一个Polygon
                IPolygon iPolygon = new PolygonClass();
                IPointCollection iPointCollection = iPolygon as IPointCollection;
                for (int i=0;i<PointCount;i++)
                {
                    IPoint pt = new PointClass();
                    pt.X = double.Parse(PointXArray[i]);
                    pt.Y = double.Parse(PointYArray[i]);
                    iPointCollection.AddPoint(pt);
                }

                IFeatureWorkspace iFeatureWorkspace = m_SDEUtils.m_WorkSpace as IFeatureWorkspace;
                IFeatureClass iFeatureClass = null;
                IFeature iFeature = null;

                //如果存在于地籍子区(完全落入)内则使用该地籍子区代码去调用取号接口，取一次号然后返回
                iFeatureClass = iFeatureWorkspace.OpenFeatureClass(g_strDJZQTableName);
                iFeature = isGeometrySearched(iFeatureClass, iPolygon, esriSpatialRelEnum.esriSpatialRelWithin);
                if (iFeature != null)
                {
                    string strDJZQDM = (string)iFeature.get_Value(iFeature.Fields.FindField("DJZQDM"));
                    return GetNewZDXH(strDJZQDM);
                }
                else//如果存在于地籍区(完全落入)内则使用该地籍区代码去调用取号接口，取一次号然后返回
                {
                    iFeatureClass = iFeatureWorkspace.OpenFeatureClass(g_strDJQTableName);
                    iFeature = isGeometrySearched(iFeatureClass, iPolygon, esriSpatialRelEnum.esriSpatialRelWithin);
                    if (iFeature != null)
                    {
                        string strDJZQDM = (string)iFeature.get_Value(iFeature.Fields.FindField("DJQDM"));
                        return GetNewZDXH(strDJZQDM + "000");
                    }
                }
                return "输入的面未落入任何地籍区或地籍子区";
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                m_SDEUtils.ReleaseResource();
            }
        }

        private IFeature isGeometrySearched(IFeatureClass iFeatureClass ,IGeometry searchGeo, esriSpatialRelEnum SpatialRel)
        {
            IFeatureCursor iFeatureCursor = null;
            IFeature iFeature = null;
            ISpatialFilter iSpatialFilter = null;
            try
            {
                iSpatialFilter = new SpatialFilter();
	            iSpatialFilter.Geometry = searchGeo;
	            iSpatialFilter.SpatialRel = SpatialRel;
	            iFeatureCursor = iFeatureClass.Search(iSpatialFilter, false);
                iFeature = iFeatureCursor.NextFeature();
	            if (iFeature != null)
	            {
                    return iFeature;
	            }
	            else
	            {
	                return null;
	            }
            }
            catch
            {
                return null;
            }
            finally
            {
                //注意不要释放了IFeature外面还要用
                m_SDEUtils.ReleaseComObject(iFeatureCursor);
                m_SDEUtils.ReleaseComObject(iSpatialFilter);
            }
        }

        [WebMethod(Description = "获取所有地籍区代码")]
        public string GetAllDJQDM()
        {
            try
            {
                if (!m_SDEUtils.ConnectToSDE())
                {
                    throw new Exception("SDE连接失败");
                }

                IFeatureWorkspace iFeatureWorkspace = m_SDEUtils.m_WorkSpace as IFeatureWorkspace;
                IFeatureClass iFeatureClass = iFeatureWorkspace.OpenFeatureClass(g_strDJQTableName);
                IFeatureCursor iFeatureCursor = iFeatureClass.Search(null, false);
                List<string> DJQDMList = new List<string>();
                IFeature iFeature = iFeatureCursor.NextFeature();
                while (iFeature != null)
                {
                    DJQDMList.Add((string)iFeature.get_Value(iFeature.Fields.FindField("DJQDM")));
                    iFeature = iFeatureCursor.NextFeature();
                }
                return string.Join(",", DJQDMList.ToArray());
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                m_SDEUtils.ReleaseResource();
            }
        }

        [WebMethod(Description = "输入一个地籍区代码，获取其所有地籍子区代码")]
        public string GetAllDJZQDMInDJQ(string strDJQDM)
        {
            try
            {
                if (!m_SDEUtils.ConnectToSDE())
                {
                    throw new Exception("SDE连接失败");
                }

                IFeatureWorkspace iFeatureWorkspace = m_SDEUtils.m_WorkSpace as IFeatureWorkspace;
                IFeatureClass iFeatureClass = iFeatureWorkspace.OpenFeatureClass(g_strDJZQTableName);
                IQueryFilter iQueryFilter = new QueryFilter();
                iQueryFilter.WhereClause = "DJQDM = '" + strDJQDM + "'";
                IFeatureCursor iFeatureCursor = iFeatureClass.Search(iQueryFilter, false);
                List<string> strList = new List<string>();
                IFeature iFeature = iFeatureCursor.NextFeature();
                while (iFeature != null)
                {
                    strList.Add((string)iFeature.get_Value(iFeature.Fields.FindField("DJZQDM")));
                    iFeature = iFeatureCursor.NextFeature();
                }
                return string.Join(",", strList.ToArray());
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                m_SDEUtils.ReleaseResource();
            }
        }

        [WebMethod(Description = "输入一个地籍区代码，获取其已取的最大代码")]
        public string GetMaxDJQDM(string strDJQDM)
        {
            try
            {
                if (!m_SDEUtils.ConnectToSDE())
                {
                    throw new Exception("SDE连接失败");
                }

                IFeatureWorkspace iFeatureWorkspace = m_SDEUtils.m_WorkSpace as IFeatureWorkspace;
                IFeatureClass iFeatureClass = iFeatureWorkspace.OpenFeatureClass(g_strDJZQTableName);
                IQueryFilter iQueryFilter = new QueryFilter();
                iQueryFilter.WhereClause = "DJQDM = '" + strDJQDM + "'";
                IFeatureCursor iFeatureCursor = iFeatureClass.Search(iQueryFilter, false);
                List<int> strList = new List<int>();
                IFeature iFeature = iFeatureCursor.NextFeature();
                while (iFeature != null)
                {
                    strList.Add((int)iFeature.get_Value(iFeature.Fields.FindField("DJZQDM")));
                    iFeature = iFeatureCursor.NextFeature();
                }

                strList.Sort();
                return strList[strList.Count - 1].ToString();
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                m_SDEUtils.ReleaseResource();
            }
        }

        [WebMethod(Description = "输入一个子地籍区代码，获取其已取的最大宗地序号")]
        public string GetMaxDJZQDM(string strDJZQDM)
        {
            try
            {
                //连接数据库
                if (!m_OracleUtils.ConnectToOracle())
                {
                    throw new Exception("Oracle连接失败");
                }

                string strSequenceName = "DJZQSEQUENCE_" + strDJZQDM;

                //判断：如果当前用户输入已经创建了Oracle序列，则取到当前序列+1值，如果没有则创建当前Oracle序列
                string strSQL = "SELECT count(*) FROM All_Sequences where Sequence_name='" + strSequenceName + "'";

                if (m_OracleUtils.GetRowSingleField(strSQL).ToString() == "0")
                {
                    return "该地籍子区未申请任何宗地序号";
                }

                //获取最大宗地序列号
                strSQL = "SELECT " + strSequenceName + ".CURRVAL FROM DUAL";
                string strXH = m_OracleUtils.GetRowSingleField(strSQL).ToString();

                return strXH;
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                m_OracleUtils.ReleaseResource();
            }
        }

        private string GetSDETableFieldValue(string strWhere,string strTableName,string strTargetField)
        {
            IFeatureClass iFeatureClass = null;
            IQueryFilter iQueryFilter = null;
            IFeatureCursor iFeatureCursor = null;
            IFeature iFeature = null;
            try
            {
	            IFeatureWorkspace iFeatureWorkspace = m_SDEUtils.m_WorkSpace as IFeatureWorkspace;
	            iFeatureClass = iFeatureWorkspace.OpenFeatureClass(strTableName);
	            iQueryFilter = new QueryFilter();
	            iQueryFilter.WhereClause = strWhere;
	            iFeatureCursor = iFeatureClass.Search(iQueryFilter, false);
	            iFeature = iFeatureCursor.NextFeature();
	            if (iFeature != null)
	            {
	                return (string)iFeature.get_Value(iFeature.Fields.FindField(strTargetField));
	            }
	            else
	            {
	                return "";
	            }
            }
            catch
            {
                return "";
            }
            finally
            {
                m_SDEUtils.ReleaseComObject(iFeature);
                m_SDEUtils.ReleaseComObject(iFeatureCursor);
                m_SDEUtils.ReleaseComObject(iQueryFilter);
                m_SDEUtils.ReleaseComObject(iFeatureClass);
            }
        }

        [WebMethod(Description = "根据地籍区代码返回地籍区名")]
        public string GetDJQDMByName(string strDJQDM)
        {
            try
            {
                if (!m_SDEUtils.ConnectToSDE())
                {
                    throw new Exception("SDE连接失败");
                }
                return GetSDETableFieldValue("DJQDM = '" + strDJQDM + "'", g_strDJQTableName, "DJQMC");
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                m_SDEUtils.ReleaseResource();
            }
        }

        [WebMethod(Description = "根据地籍区名，返回地籍区代码")]
        public string GetNameByDJQDM(string strName)
        {
            try
            {
                if (!m_SDEUtils.ConnectToSDE())
                {
                    throw new Exception("SDE连接失败");
                }
                return GetSDETableFieldValue("DJQMC = '" + strName + "'", g_strDJQTableName, "DJQDM");
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                m_SDEUtils.ReleaseResource();
            }
        }

        [WebMethod(Description = "根据地籍子区代码返回地籍子区名")]
        public string GetDJZQDMByName(string strDJZQDM)
        {
            try
            {
                if (!m_SDEUtils.ConnectToSDE())
                {
                    throw new Exception("SDE连接失败");
                }
                return GetSDETableFieldValue("DJZQDM = '" + strDJZQDM + "'", g_strDJZQTableName, "DJZQMC");
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                m_SDEUtils.ReleaseResource();
            }
        }

        [WebMethod(Description = "根据地籍子区名，返回地籍子区代码")]
        public string GetNameByDJZQDM(string strName)
        {
            try
            {
                if (!m_SDEUtils.ConnectToSDE())
                {
                    throw new Exception("SDE连接失败");
                }
                return GetSDETableFieldValue("DJZQMC = '" + strName + "'", g_strDJZQTableName, "DJZQDM");
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                m_SDEUtils.ReleaseResource();
            }
        }
    }
}