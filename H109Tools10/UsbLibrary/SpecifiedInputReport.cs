namespace UsbLibrary
{
    using System;

    public class SpecifiedInputReport : InputReport
    {
        private byte[] arrData;

        public SpecifiedInputReport(HIDDevice oDev) : base(oDev)
        {
        }

        public override void ProcessData()
        {
            this.arrData = base.Buffer;
        }

        public byte[] Data =>
            this.arrData;
    }
}

