namespace UsbLibrary
{
    using System;
    using System.ComponentModel;
    using System.Threading;

    public class UsbHidPort : Component
    {
        private IContainer components;
        private Guid device_class;
        private IntPtr handle;
        private int product_id;
        private UsbLibrary.SpecifiedDevice specified_device;
        private IntPtr usb_event_handle;
        private int vendor_id;

        [DisplayName("OnDataRecieved"), Description("The event that occurs when data is recieved from the embedded system"), Category("Embedded Event")]
        public event DataRecievedEventHandler OnDataRecieved;

        [DisplayName("OnDataSend"), Category("Embedded Event"), Description("The event that occurs when data is send from the host to the embedded system")]
        public event EventHandler OnDataSend;

        [Category("Embedded Event"), Description("The event that occurs when a usb hid device is found on the bus"), DisplayName("OnDeviceArrived")]
        public event EventHandler OnDeviceArrived;

        [Description("The event that occurs when a usb hid device is removed from the bus"), DisplayName("OnDeviceRemoved"), Category("Embedded Event")]
        public event EventHandler OnDeviceRemoved;

        [DisplayName("OnSpecifiedDeviceArrived"), Description("The event that occurs when a usb hid device with the specified vendor id and product id is found on the bus"), Category("Embedded Event")]
        public event EventHandler OnSpecifiedDeviceArrived;

        [DisplayName("OnSpecifiedDeviceRemoved"), Category("Embedded Event"), Description("The event that occurs when a usb hid device with the specified vendor id and product id is removed from the bus")]
        public event EventHandler OnSpecifiedDeviceRemoved;

        public UsbHidPort()
        {
            this.product_id = 0;
            this.vendor_id = 0;
            this.specified_device = null;
            this.device_class = Win32Usb.HIDGuid;
            this.InitializeComponent();
        }

        public UsbHidPort(IContainer container)
        {
            this.product_id = 0;
            this.vendor_id = 0;
            this.specified_device = null;
            this.device_class = Win32Usb.HIDGuid;
            container.Add(this);
            this.InitializeComponent();
        }

        public void CheckDevicePresent()
        {
            try
            {
                bool flag = false;
                if (this.specified_device != null)
                {
                    flag = true;
                }
                this.specified_device = UsbLibrary.SpecifiedDevice.FindSpecifiedDevice(this.vendor_id, this.product_id);
                if (this.specified_device != null)
                {
                    if (this.OnSpecifiedDeviceArrived != null)
                    {
                        this.OnSpecifiedDeviceArrived(this, new EventArgs());
                        this.specified_device.DataSend += new DataSendEventHandler(this.OnDataSend.Invoke);
                        this.specified_device.DataRecieved += new DataRecievedEventHandler(this.OnDataRecieved.Invoke);
                    }
                }
                else if ((this.OnSpecifiedDeviceRemoved != null) && flag)
                {
                    this.OnSpecifiedDeviceRemoved(this, new EventArgs());
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }

        private void DataRecieved(object sender, DataRecievedEventArgs args)
        {
            if (this.OnDataRecieved != null)
            {
                this.OnDataRecieved(sender, args);
            }
        }

        private void DataSend(object sender, DataSendEventArgs args)
        {
            if (this.OnDataSend != null)
            {
                this.OnDataSend(sender, args);
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.UnregisterHandle();
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new Container();
        }

        public void ParseMessages(int m, IntPtr WParam)
        {
            if (m == 0x219)
            {
                int num = WParam.ToInt32();
                if (num == 0x8000)
                {
                    if (this.OnDeviceArrived != null)
                    {
                        this.OnDeviceArrived(this, new EventArgs());
                        this.CheckDevicePresent();
                    }
                }
                else if ((num == 0x8004) && (this.OnDeviceRemoved != null))
                {
                    this.OnDeviceRemoved(this, new EventArgs());
                    this.CheckDevicePresent();
                }
            }
        }

        public void RegisterHandle(IntPtr Handle)
        {
            this.usb_event_handle = Win32Usb.RegisterForUsbEvents(Handle, this.device_class);
            this.handle = Handle;
            this.CheckDevicePresent();
        }

        public bool UnregisterHandle() => 
            Win32Usb.UnregisterForUsbEvents(this.handle);

        [DefaultValue("(none)"), Description("The Device Class the USB device belongs to"), Category("Embedded Details")]
        public Guid DeviceClass =>
            this.device_class;

        [Category("Embedded Details"), Description("The product id from the USB device you want to use"), DefaultValue("(none)")]
        public int ProductId
        {
            get
            {
                return this.product_id;
            }
            set
            {
                this.product_id = value;
            }
        }

        [DefaultValue("(none)"), Description("The Device witch applies to the specifications you set"), Category("Embedded Details")]
        public UsbLibrary.SpecifiedDevice SpecifiedDevice =>
            this.specified_device;

        [Description("The vendor id from the USB device you want to use"), Category("Embedded Details"), DefaultValue("(none)")]
        public int VendorId
        {
            get
            {
                return this.vendor_id;
            }
            set
            {
                this.vendor_id = value;
            }
        }
    }
}

