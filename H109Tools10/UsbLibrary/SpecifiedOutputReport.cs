namespace UsbLibrary
{
    using System;

    public class SpecifiedOutputReport : OutputReport
    {
        public SpecifiedOutputReport(HIDDevice oDev) : base(oDev)
        {
        }

        public bool SendData(byte[] data)
        {
            byte[] buffer = base.Buffer;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = data[i];
            }
            if (buffer.Length < data.Length)
            {
                return false;
            }
            return true;
        }
    }
}

