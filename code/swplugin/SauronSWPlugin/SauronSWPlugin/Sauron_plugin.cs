using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swcommands;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;
using SolidWorksTools;

namespace SauronSWPlugin
{
    public class Sauron_plugin : ISwAddin
    {
        /*
         * COM registration stuff
         */

        [ComRegisterFunction]
        public static void ComRegister(Type t)
        {
            string keyPath = String.Format(@"SOFTWARE\SolidWorks\AddIns\{0:b}", t.GUID);
            using (Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(keyPath))
            {
                rk.SetValue(null, 1); // Load at startup
                rk.SetValue("Title", "Sauron");
                rk.SetValue("Description", "computer vision-ize your models");
            }
        }
        [ComUnregisterFunction]
        public static void ComUnregister(Type t)
        {
            string keyPath = String.Format(@"SOFTWARE\SolidWorks\AddIns\{0:b}", t.GUID);
            Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(keyPath);
        }
        
        /*
         * Begin useful class stuff!
         */

        public SldWorks mSWApplication;
        private int mSWCookie;

        private TaskpaneView mTaskpaneView;
        private SWTaskpaneHost mTaskpaneHost;

        public bool ConnectToSW(object ThisSW, int Cookie)
        {
            mSWApplication = (SldWorks)ThisSW;
            mSWCookie = Cookie;
            // set up add-in callback info
            bool result = mSWApplication.SetAddinCallbackInfo(0, this, Cookie);
            this.UISetup();
            return true;
        }

        public bool DisconnectFromSW()
        {
            this.UITeardown();
            return true;
        }

        private void UISetup()
        {
            mTaskpaneView = mSWApplication.CreateTaskpaneView2(string.Empty, "sauron: computer vision-ize your models");
            mTaskpaneHost = (SWTaskpaneHost)mTaskpaneView.AddControl(SWTaskpaneHost.SWTASKPANE_PROGID, "");
            mTaskpaneHost.swApp = mSWApplication;
        }

        private void UITeardown()
        {
            mTaskpaneHost = null;
            mTaskpaneView.DeleteView();
            Marshal.ReleaseComObject(mTaskpaneView);
            mTaskpaneView = null;
        }
    }
}
