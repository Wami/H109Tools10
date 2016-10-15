namespace UsbLibrary
{
    using System;

    public abstract class InputReport : Report
    {
        public InputReport(HIDDevice oDev) : base(oDev)
        {
        }

        public abstract void ProcessData();
        public void SetData(byte[] arrData)
        {
            base.SetBuffer(arrData);
            this.ProcessData();
        }
    }
}

