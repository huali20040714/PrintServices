using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.ComponentModel;
using System.Management;

namespace PrintServices
{
    public class XPSPrintException : Exception
    {
        public XPSPrintException() : base() { }
        public XPSPrintException(string message) : base(message) { }
        public XPSPrintException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class VirtualPrinter : IDisposable
    {
        #region API Delcarations
        private const uint PRINTER_ATTRIBUTE_QUEUED = 0x00000001;
        private const uint PRINTER_ATTRIBUTE_DIRECT = 0x00000002;
        private const uint PRINTER_ATTRIBUTE_DEFAULT = 0x00000004;
        private const uint PRINTER_ATTRIBUTE_SHARED = 0x00000008;
        private const uint PRINTER_ATTRIBUTE_NETWORK = 0x00000010;
        private const uint PRINTER_ATTRIBUTE_HIDDEN = 0x00000020;
        private const uint PRINTER_ATTRIBUTE_LOCAL = 0x00000040;

        private const uint PRINTER_ATTRIBUTE_ENABLE_DEVQ = 0x00000080;
        private const uint PRINTER_ATTRIBUTE_KEEPPRINTEDJOBS = 0x00000100;
        private const uint PRINTER_ATTRIBUTE_DO_COMPLETE_FIRST = 0x00000200;

        private const uint PRINTER_ATTRIBUTE_WORK_OFFLINE = 0x00000400;
        private const uint PRINTER_ATTRIBUTE_ENABLE_BIDI = 0x00000800;
        private const uint PRINTER_ATTRIBUTE_RAW_ONLY = 0x00001000;
        private const uint PRINTER_ATTRIBUTE_PUBLISHED = 0x00002000;
        private const uint PRINTER_ATTRIBUTE_FAX = 0x00004000;
        private const uint PRINTER_ATTRIBUTE_TS = 0x00008000;

        private const uint PRINTER_ALL_ACCESS = 0x000F000C;

        private const int ERROR_FILE_NOT_FOUND = 2;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        private const short CCDEVICENAME = 32;
        private const short CCFORMNAME = 32;

        private const int DM_IN_BUFFER = 8;
        private const int DM_OUT_BUFFER = 2;

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public short dmOrientation;
            public short dmPaperSize;
            public short dmPaperLength;
            public short dmPaperWidth;
            public short dmScale;
            public short dmCopies;
            public short dmDefaultSource;
            public short dmPrintQuality;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCFORMNAME)]
            public string dmFormName;
            public short dmUnusedPadding;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PRINTER_INFO_2
        {
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pServerName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pPrinterName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pShareName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pPortName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDriverName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pComment;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pLocation;
            public IntPtr pDevMode;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pSepFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pPrintProcessor;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDatatype;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pParameters;

            public IntPtr pSecurityDescriptor;
            public uint Attributes;
            public uint Priority;
            public uint DefaultPriority;
            public uint StartTime;
            public uint UntilTime;
            public uint Status;
            public uint cJobs;
            public uint AveragePPM;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PRINTER_INFO_5
        {
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pPrinterName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pPortName;
            public uint Attributes;
            public uint DeviceNotSelectedTimeout;
            public uint TransmissionRetryTimeout;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct PRINTER_DEFAULTS
        {
            public IntPtr pDatatype;
            public IntPtr pDevMode;
            public uint DesiredAccess;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PORT_INFO_1
        {
            [MarshalAs(UnmanagedType.LPTStr)]
            public string szPortName;
        }

        [DllImport("winspool.drv", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern IntPtr AddPrinter(string pName, uint Level, [In] ref PRINTER_INFO_2 pPrinter);

        [DllImport("winspool.drv")]
        private static extern int ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, ref PRINTER_DEFAULTS pDefault);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool DeletePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetDefaultPrinter(StringBuilder pszBuffer, ref int pcchBuffer);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int AddPortEx(string pName, int pLevel, ref PORT_INFO_1 lpBuffer, string pMonitorName);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int DeletePort(string pName, IntPtr hWnd, string pPortName);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumPorts(string pName, uint level, IntPtr lpbPorts, uint cbBuf, ref uint pcbNeeded, ref uint pcReturned);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetDefaultPrinter(string printerName);


        #endregion

        #region Helpers

        private string[] GetInstalledPorts()
        {
            uint pcbNeeded = 0;
            uint pcReturned = 0;

            if (EnumPorts(null, 1, IntPtr.Zero, 0, ref pcbNeeded, ref pcReturned))
            {
                //succeeds, but must not, because buffer is zero (too small)!
                return null;
            }

            int lastWin32Error = Marshal.GetLastWin32Error();
            //ERROR_INSUFFICIENT_BUFFER expected, if not -> Exception
            if (lastWin32Error != ERROR_INSUFFICIENT_BUFFER)
            {
                throw new Win32Exception(lastWin32Error);
            }

            IntPtr pPorts = Marshal.AllocHGlobal((int)pcbNeeded);
            if (EnumPorts(null, 1, pPorts, pcbNeeded, ref pcbNeeded, ref pcReturned))
            {
                IntPtr currentPort = pPorts;
                string[] sPorts = new string[pcReturned];
                for (int i = 0; i < pcReturned; i++)
                {
                    PORT_INFO_1 pinfo = (PORT_INFO_1)Marshal.PtrToStructure(currentPort, typeof(PORT_INFO_1));
                    sPorts[i] = pinfo.szPortName;
                    currentPort = (IntPtr)(currentPort.ToInt32() + Marshal.SizeOf(typeof(PORT_INFO_1)));
                }
                Marshal.FreeHGlobal(pPorts);
                return sPorts;
            }
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private bool PortExists(string portName)
        {
            string[] ports = GetInstalledPorts();
            foreach (string port in ports)
            {
                if (port.Equals(portName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private bool PrinterExists(string printerName)
        {
            IntPtr hPrinter = IntPtr.Zero;
            PRINTER_DEFAULTS defaults = new PRINTER_DEFAULTS
            {
                DesiredAccess = PRINTER_ALL_ACCESS,
                pDatatype = IntPtr.Zero,
                pDevMode = IntPtr.Zero
            };
            try
            {
                if (OpenPrinter(printerName, out hPrinter, ref defaults))
                {
                    ClosePrinter(hPrinter);
                    return hPrinter != IntPtr.Zero;
                }
            }
            catch { }
            return false;
        }

        public static string GetDefaultPrinter()
        {
            int lastError = 0;
            int pcchBuffer = 0;
            if (GetDefaultPrinter(null, ref pcchBuffer))
            {
                return null;
            }
            lastError = Marshal.GetLastWin32Error();
            if (lastError == ERROR_INSUFFICIENT_BUFFER)
            {
                StringBuilder pszBuffer = new StringBuilder(pcchBuffer);
                if (GetDefaultPrinter(pszBuffer, ref pcchBuffer))
                {
                    return pszBuffer.ToString();
                }
                lastError = Marshal.GetLastWin32Error();
            }
            if (lastError == ERROR_FILE_NOT_FOUND)
            {
                return null;
            }
            throw new Win32Exception(lastError);
        }

        #endregion

        #region Default Settings

        private static string _xpsDriver = "Passthrough XPS";
        private static string _winProcessor = "winprint";
        private static string _portMonitor = "Local Port";
        private static string _defaultPort = "FILE:";

        public static string XPSPrintDriver
        {
            get { return _xpsDriver; }
            set { _xpsDriver = value; }
        }
        public static string WinProcessor
        {
            get { return _winProcessor; }
            set { _winProcessor = value; }
        }
        public static string PortMonitor
        {
            get { return _portMonitor; }
            set { _portMonitor = value; }
        }
        public static string DefaultPort
        {
            get { return _defaultPort; }
            set { _defaultPort = value; }
        }

        #endregion

        #region Instance members
        private string _printerName;
        private string _server = null;
        private string _monitorName = _portMonitor;
        private string _processorName = _winProcessor;
        private string _port;
        private string _driverName = _xpsDriver;
        private bool _printerCreated = false;
        private bool _portCreated = false;

        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        public string Driver
        {
            get { return _driverName; }
            set { _driverName = value; }
        }

        public string Monitor
        {
            get { return _monitorName; }
            set { _monitorName = value; }
        }

        public string Processor
        {
            get { return _processorName; }
            set { _processorName = value; }
        }

        public string Port
        {
            get
            {
                return _port;
            }
            set
            {
                SetPort(value);
            }
        }

        #endregion

        #region Constructors

        public VirtualPrinter(string printerName, string portName)
        {
            bool create = !PortExists(portName);
            if (create)
            {
                AddPort(portName);
                _portCreated = true;
            }
            _port = portName;
            create = !PrinterExists(printerName);
            if (create)
            {
                CreatePrinter(printerName, portName);
                _printerCreated = true;
            }
            else
                SetPort(portName);
            _printerName = printerName;
        }

        public VirtualPrinter(string printerName) : this(printerName, _defaultPort) { }

        #endregion

        #region Port Functions

        private void AddPort(string portName)
        {

            PORT_INFO_1 pInfo = new PORT_INFO_1 { szPortName = portName };
            int nResult = AddPortEx(null, 1, ref pInfo, _portMonitor);
            if (nResult == 0)
            {
                nResult = Marshal.GetLastWin32Error();
                if (nResult == 87)
                {    // Returned if the port exists - but may be for other circumstances too
                    // Double check that the port does, in fact, exist
                    if (PortExists(portName))
                        throw new XPSPrintException(string.Format("Port {0} already exists", portName));
                }
                throw new Win32Exception(nResult);
            }
        }

        private void DeletePort(string portName)
        {
            if (PortExists(portName))
            {
                int nResult = DeletePort(null, IntPtr.Zero, portName);
                if (nResult == 0)
                {
                    nResult = Marshal.GetLastWin32Error();
                    throw new Win32Exception(nResult);
                }
            }
            else
                throw new XPSPrintException(string.Format("Local Port {0} not found", portName));
        }

        public void SetPort(string portName)
        {
            bool created = false;
            if (!PortExists(portName))
            {
                AddPort(portName);
                created = true;
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer WHERE Name='" + _printerName + "'");
            foreach (ManagementObject printer in searcher.Get())
            {
                printer["PortName"] = portName;
                printer.Put();  // Important: Call put to save the settings. 
            }
            if (_portCreated && !string.IsNullOrEmpty(_port) && !_port.Equals(portName))
            {
                DeletePort(_port);
            }
            _port = portName;
            _portCreated = created;
        }
        
        #endregion

        #region Printer Functions

        private void CreatePrinter(string printerName, string portName)
        {
            PRINTER_INFO_2 _pInfo = new PRINTER_INFO_2
            {
                pPrinterName = printerName,
                pPortName = portName,
                pDriverName = _xpsDriver,
                pPrintProcessor = _winProcessor
            };
            IntPtr hPrinter = AddPrinter(null, 2, ref _pInfo);
            if (hPrinter != IntPtr.Zero)
            {
                ClosePrinter(hPrinter);
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private void DeletePrinter()
        {
            IntPtr hPrinter = IntPtr.Zero;
            PRINTER_DEFAULTS pDefaults = new PRINTER_DEFAULTS
            {
                DesiredAccess = PRINTER_ALL_ACCESS,
                pDatatype = IntPtr.Zero,
                pDevMode = IntPtr.Zero
            };
            Exception failure = null;

            for (int nretries = 0; ; nretries++)
            {
                // Set the port to the default port before deleting the printer.
                // Otherwise, the delete may fail because the port is in use
                try
                {
                    SetPort(_defaultPort);
                }
                catch { }
                if (OpenPrinter(_printerName, out hPrinter, ref pDefaults))
                {
                    try
                    {
                        // Retry the deletion of the printer for a second or so, just in case the spooler has not had time to clean up its last print job
                        if (!DeletePrinter(hPrinter))
                        {
                            if (nretries < 10)
                            {
                                Thread.Sleep(100);
                                continue;
                            }
                            failure = new Win32Exception(Marshal.GetLastWin32Error());
                        }
                        break;
                    }
                    finally
                    {
                        ClosePrinter(hPrinter);
                    }
                }
                else
                {
                    failure = new Win32Exception(Marshal.GetLastWin32Error());
                    break;
                }
            }
            if (failure != null)
                if (PrinterExists(_printerName))
                    throw failure;
        }

        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            bool _printerDeleted = false;
            if (_printerCreated && !string.IsNullOrEmpty(_printerName))
            {
                try
                {
                    DeletePrinter();
                    _printerDeleted = true;
                    _printerName = null;
                    _printerCreated = false;
                }
                catch { }
            }
            if (_portCreated && !string.IsNullOrEmpty(_port))
            {
                if (!_printerDeleted && PrinterExists(_printerName))
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer WHERE Name='" + _printerName + "'");
                    foreach (ManagementObject printer in searcher.Get())
                    {
                        if (_port.Equals(printer["PortName"]))
                        {
                            printer["PortName"] = _defaultPort;
                            printer.Put();
                        }
                    }
                    try
                    {
                        DeletePort(_port);
                        _port = null;
                        _portCreated = false;
                    }
                    catch { }
                }
            }
        }
        #endregion
    }
}

