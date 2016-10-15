namespace UsbLibrary
{
    using Microsoft.Win32.SafeHandles;
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;

    public abstract class HIDDevice : Win32Usb, IDisposable
    {
        private IntPtr m_hHandle;
        private int m_nInputReportLength;
        private int m_nOutputReportLength;
        private FileStream m_oFile;

        public event EventHandler OnDeviceRemoved;

        protected HIDDevice()
        {
        }

        private void BeginAsyncRead()
        {
            byte[] buffer = new byte[this.m_nInputReportLength];
            this.m_oFile.BeginRead(buffer, 0, this.m_nInputReportLength, new AsyncCallback(this.ReadCompleted), buffer);
        }

        public virtual InputReport CreateInputReport() => 
            null;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool bDisposing)
        {
            try
            {
                if (bDisposing && (this.m_oFile != null))
                {
                    this.m_oFile.Close();
                    this.m_oFile = null;
                }
                if (this.m_hHandle != IntPtr.Zero)
                {
                    Win32Usb.CloseHandle(this.m_hHandle);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }

        public static HIDDevice FindDevice(int nVid, int nPid, Type oType)
        {
            string str = $"vid_{nVid:x4}&pid_{nPid:x4}";
            Guid hIDGuid = Win32Usb.HIDGuid;
            IntPtr lpDeviceInfoSet = Win32Usb.SetupDiGetClassDevs(ref hIDGuid, null, IntPtr.Zero, 0x12);
            try
            {
                Win32Usb.DeviceInterfaceData structure = new Win32Usb.DeviceInterfaceData();
                structure.Size = Marshal.SizeOf(structure);
                for (int i = 0; Win32Usb.SetupDiEnumDeviceInterfaces(lpDeviceInfoSet, 0, ref hIDGuid, (uint) i, ref structure); i++)
                {
                    string devicePath = GetDevicePath(lpDeviceInfoSet, ref structure);
                    if (devicePath.IndexOf(str) >= 0)
                    {
                        HIDDevice device = (HIDDevice) Activator.CreateInstance(oType);
                        device.Initialise(devicePath);
                        return device;
                    }
                }
            }
            catch (Exception exception)
            {
                throw HIDDeviceException.GenerateError(exception.ToString());
            }
            finally
            {
                Win32Usb.SetupDiDestroyDeviceInfoList(lpDeviceInfoSet);
            }
            return null;
        }

        private static string GetDevicePath(IntPtr hInfoSet, ref Win32Usb.DeviceInterfaceData oInterface)
        {
            uint nRequiredSize = 0;
            if (!Win32Usb.SetupDiGetDeviceInterfaceDetail(hInfoSet, ref oInterface, IntPtr.Zero, 0, ref nRequiredSize, IntPtr.Zero))
            {
                Win32Usb.DeviceInterfaceDetailData oDetailData = new Win32Usb.DeviceInterfaceDetailData {
                    Size = 5
                };
                if (Win32Usb.SetupDiGetDeviceInterfaceDetail(hInfoSet, ref oInterface, ref oDetailData, nRequiredSize, ref nRequiredSize, IntPtr.Zero))
                {
                    return oDetailData.DevicePath;
                }
            }
            return null;
        }

        protected virtual void HandleDataReceived(InputReport oInRep)
        {
        }

        protected virtual void HandleDeviceRemoved()
        {
        }

        private void Initialise(string strPath)
        {
            this.m_hHandle = Win32Usb.CreateFile(strPath, 0xc0000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
            if (this.m_hHandle != Win32Usb.InvalidHandleValue)
            {
                IntPtr ptr;
                if (Win32Usb.HidD_GetPreparsedData(this.m_hHandle, out ptr))
                {
                    try
                    {
                        try
                        {
                            Win32Usb.HidCaps caps;
                            Win32Usb.HidP_GetCaps(ptr, out caps);
                            this.m_nInputReportLength = caps.InputReportByteLength;
                            this.m_nOutputReportLength = caps.OutputReportByteLength;
                            this.m_oFile = new FileStream(new SafeFileHandle(this.m_hHandle, false), FileAccess.ReadWrite, this.m_nInputReportLength, true);
                            this.BeginAsyncRead();
                        }
                        catch (Exception)
                        {
                            throw HIDDeviceException.GenerateWithWinError("Failed to get the detailed data from the hid.");
                        }
                        return;
                    }
                    finally
                    {
                        Win32Usb.HidD_FreePreparsedData(ref ptr);
                    }
                }
                throw HIDDeviceException.GenerateWithWinError("GetPreparsedData failed");
            }
            this.m_hHandle = IntPtr.Zero;
            throw HIDDeviceException.GenerateWithWinError("Failed to create device file");
        }

        protected void ReadCompleted(IAsyncResult iResult)
        {
            byte[] asyncState = (byte[]) iResult.AsyncState;
            try
            {
                this.m_oFile.EndRead(iResult);
                try
                {
                    InputReport oInRep = this.CreateInputReport();
                    oInRep.SetData(asyncState);
                    this.HandleDataReceived(oInRep);
                }
                finally
                {
                    this.BeginAsyncRead();
                }
            }
            catch (IOException)
            {
                this.HandleDeviceRemoved();
                if (this.OnDeviceRemoved != null)
                {
                    this.OnDeviceRemoved(this, new EventArgs());
                }
                this.Dispose();
            }
        }

        protected void Write(OutputReport oOutRep)
        {
            try
            {
                this.m_oFile.Write(oOutRep.Buffer, 0, oOutRep.BufferLength);
            }
            catch (IOException)
            {
                throw new HIDDeviceException("Probbaly the device was removed");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }

        public int InputReportLength =>
            this.m_nInputReportLength;

        public int OutputReportLength =>
            this.m_nOutputReportLength;
    }
}

