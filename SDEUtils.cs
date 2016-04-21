using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;

namespace NanNingWebServiceSpace
{
    class SDEUtils
    {
        public IWorkspace m_WorkSpace = null;
        public bool ConnectToSDE()
        {
            if (!_IdentifySDE())
            {
                return false;
            }
            string SERVER = ConfigurationManager.AppSettings.Get("SDE_Server");
            string INSTANCE = ConfigurationManager.AppSettings.Get("SDE_Instance");
            string DATABASE = ConfigurationManager.AppSettings.Get("SDE_Database");
            string USER = ConfigurationManager.AppSettings.Get("SDE_User");
            string PASSWORD = ConfigurationManager.AppSettings.Get("SDE_Password");
            string VERSION = ConfigurationManager.AppSettings.Get("SDE_Version");

            IPropertySet property = new PropertySet();

            property.SetProperty("SERVER", SERVER);
            property.SetProperty("INSTANCE", INSTANCE);
            property.SetProperty("DATABASE", DATABASE);
            property.SetProperty("USER", USER);
            property.SetProperty("PASSWORD", PASSWORD);
            property.SetProperty("VERSION", VERSION);

            try
            {
                Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.SDEWorkspaceFactory");

                IWorkspaceFactory m_WorkSpaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);

                m_WorkSpace = m_WorkSpaceFactory.Open(property, 0);

                IVersion version = m_WorkSpace as IVersion;

                version.RefreshVersion();

                if (m_WorkSpace == null)
                {
                    return false;
                }

                return true;
            }
            catch(Exception ex)
            {
                string sr = ex.Message;
            	return false;
            }
        }

        public void ReleaseResource()
        {
            ReleaseComObject(m_WorkSpace);
            if (m_AoInitialize != null)
            {
                m_AoInitialize.Shutdown();
                m_AoInitialize = null;
            }
        }

        public void ReleaseComObject(object ComObj)
        {
            if (ComObj != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(ComObj);
                ComObj = null;
            }
        }

        private IAoInitialize m_AoInitialize = null;
        private bool _IdentifySDE()
        {
            try
            {
                if (!ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.Desktop))
                {
                    System.Environment.Exit(0);
                }
                m_AoInitialize = new ESRI.ArcGIS.esriSystem.AoInitialize();
                m_AoInitialize.Initialize(ESRI.ArcGIS.esriSystem.esriLicenseProductCode.esriLicenseProductCodeAdvanced);
            }
            catch
            {
            	return false;
            }
            return true;
        }
    }
}
