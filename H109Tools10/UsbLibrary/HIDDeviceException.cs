namespace UsbLibrary
{
    using System;
    using System.Runtime.InteropServices;

    public class HIDDeviceException : ApplicationException
    {
        public HIDDeviceException(string strMessage) : base(strMessage)
        {
        }

        public static HIDDeviceException GenerateError(string strMessage) => 
            new HIDDeviceException($"Msg:{strMessage}");

        public static HIDDeviceException GenerateWithWinError(string strMessage) => 
            new HIDDeviceException($"Msg:{strMessage} WinEr:{Marshal.GetLastWin32Error():X8}");
    }
}

