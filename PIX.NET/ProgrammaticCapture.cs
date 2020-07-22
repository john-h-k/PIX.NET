using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using static TerraFX.Interop.Windows;

namespace PIX.NET
{
    public static unsafe class ProgrammaticCapture
    {
        private static IDXGraphicsAnalysis* _pCapture = CreateIDXGraphicsAnalysis();

        private static unsafe IDXGraphicsAnalysis* CreateIDXGraphicsAnalysis()
        {
            IDXGraphicsAnalysis* pCapture;
            Guid iid = IID_IDXGraphicsAnalysis;
            DXGIGetDebugInterface1(0, &iid, (void**)&pCapture);
            return pCapture;
        }

        public static bool IsPixAttached => _pCapture is not null;

        public static void BeginCapture()
        {
            _pCapture->BeginCapture();
        }
        public static void EndCapture()
        {
            _pCapture->EndCapture();
        }
    }
}
