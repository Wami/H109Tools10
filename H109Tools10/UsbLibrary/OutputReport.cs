namespace UsbLibrary
{
    using System;

    public abstract class OutputReport : Report
    {
        public OutputReport(HIDDevice oDev) : base(oDev)
        {
            base.SetBuffer(new byte[oDev.OutputReportLength]);
        }
    }
}

