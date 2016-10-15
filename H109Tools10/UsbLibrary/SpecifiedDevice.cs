namespace UsbLibrary
{
    using System;
    using System.Threading;

    public class SpecifiedDevice : HIDDevice
    {
        public event DataRecievedEventHandler DataRecieved;

        public event DataSendEventHandler DataSend;

        public override InputReport CreateInputReport() => 
            new SpecifiedInputReport(this);

        protected override void Dispose(bool bDisposing)
        {
            base.Dispose(bDisposing);
        }

        public static SpecifiedDevice FindSpecifiedDevice(int vendor_id, int product_id) => 
            ((SpecifiedDevice) HIDDevice.FindDevice(vendor_id, product_id, typeof(SpecifiedDevice)));

        protected override void HandleDataReceived(InputReport oInRep)
        {
            if (this.DataRecieved != null)
            {
                SpecifiedInputReport report = (SpecifiedInputReport) oInRep;
                this.DataRecieved(this, new DataRecievedEventArgs(report.Data));
            }
        }

        public void SendData(byte[] data)
        {
            SpecifiedOutputReport oOutRep = new SpecifiedOutputReport(this);
            oOutRep.SendData(data);
            try
            {
                base.Write(oOutRep);
                if (this.DataSend != null)
                {
                    this.DataSend(this, new DataSendEventArgs(data));
                }
            }
            catch (HIDDeviceException)
            {
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }
    }
}

