namespace H109Tools10
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Windows;
    using System.Windows.Interop;
    using UsbLibrary;

    public class HidUsbCommunication
    {
        private byte AckCmd;
        
        //++not implemented
        public string[] AdcName;
        public static byte CCodeRestIMU = 0x13;
        public static byte CCodeSetAccMiddle = 0x16;
        public static byte CCodeSetCompass = 0x17;
        public static byte CCodeSetGyroTD = 0x15;
        public static byte CCodeSetGyroZero = 20;
        public static byte CI_Debug_1 = 9;
        public static byte CI_Debug_String = 2;
        public static byte CI_DebugButton1 = 14;
        public static byte CI_RCVal = 0x10;
        public static byte CI_RestSenser = 0x11;
        public static byte CI_RStat = 4;
        public static byte CI_SetBLDC = 5;
        public static byte CI_SetDac = 8;
        public static byte CI_SetKey = 0xc6;
        public static byte CI_SetRcMax = 7; 
        public static byte CI_SetSysPram2 = 11;
        public static byte CI_SetSysPram3 = 15;
        public static byte CI_SysInf = 0x13;    
        public static byte CI_TRst = 3;
        //--not implemented

        public static byte CI_Calibrate1 = 6; //!!!!!
        public static byte CI_GetAdcStr = 12;  //!!!!
        public static byte CI_GetMacDesc = 13; //get FW version from UAV
        public static byte CI_SetSysPram1 = 10; //set UAV params
        public static byte CI_UpdataMC = 0xc5;  //upload FW to UAV

        public byte[] ComBuf_R;
        public byte ComCmd;
        public int ComDataLen;
        public bool IsAckCmd;
        public bool LinkF;
        public byte UCmd_ToUsart = 2;
        public byte UCmd_UsartToUsb = 3;
        public byte[] UpdateRecData;
        private UsbHidPort usb = new UsbHidPort();
        public byte[] UsbBuf_R;
        private byte[] UsbBuf_T;
        private int UsbRCount;
        private byte UsbsFlag;
        private int UsbTCount;
        private int UsbTxCnt;
        private int UsbTxLen;
        private byte usPrevFlag;

        public event Evnet_ComComplete ComIsComplete;

        public event Event_RecDebugData DebugDataIsRec;

        public event Event_RecDisString DiscribeStringIsRec;

        public event Event_RecDebugData IsCalibration;

        public void ComComplete()
        {
            string str2 = "";
            for (int i = 0; i < this.ComDataLen; i++)
            {
                str2 = str2 + $"{this.ComBuf_R[i]:X}" + " ";
            }
            if (this.ComCmd == CI_GetMacDesc) //get FW version from UAV
            {
                // FV version: V HwNumber.Manufacturer.SwNumber
                SGlobalVariable.HwNumber = this.ComBuf_R[0];
                SGlobalVariable.Manufacturer = this.ComBuf_R[1];
                SGlobalVariable.SwNumber = this.ComBuf_R[2];

                SGlobalVariable.IdNumber = BitConverter.ToUInt32(this.ComBuf_R, 4);
                SGlobalVariable.FlightTime = BitConverter.ToUInt32(this.ComBuf_R, 8);

                SGlobalVariable.FlightDesString = Encoding.Default.GetString(this.ComBuf_R, 12, this.ComDataLen);
                SGlobalVariable.FlightDesString = SGlobalVariable.FlightDesString.Remove(SGlobalVariable.FlightDesString.IndexOf('\0'));
                SGlobalVariable.FlightDesString = SGlobalVariable.FlightDesString + " V" + SGlobalVariable.HwNumber.ToString() + "." + SGlobalVariable.Manufacturer.ToString() + "." 
                    + SGlobalVariable.SwNumber.ToString() + " IdNumber:" + SGlobalVariable.IdNumber.ToString() + " FlightTime:" + SGlobalVariable.FlightTime.ToString();
                if (this.DiscribeStringIsRec != null)
                {
                    this.DiscribeStringIsRec(SGlobalVariable.FlightDesString, CI_GetMacDesc);
                }
            }
            if (this.ComCmd == CI_GetAdcStr)
            {
                string s = Encoding.Default.GetString(this.ComBuf_R, 0, this.ComDataLen);
                if (this.DiscribeStringIsRec != null)
                {
                    this.DiscribeStringIsRec(s, CI_GetAdcStr);
                }
            }
            else if (this.ComCmd == CI_Debug_1)
            {
                if (this.DebugDataIsRec != null)
                {
                    this.DebugDataIsRec(this.ComBuf_R, this.ComDataLen);
                }
            }
            else if (this.ComCmd == CI_UpdataMC) //upload FW to UAV
            {
                this.UpdateRecData = new byte[this.ComBuf_R.Length];
                Array.Copy(this.ComBuf_R, 0, this.UpdateRecData, 0, this.ComDataLen);
            }
            else if ((this.ComCmd == CI_Calibrate1) && (this.IsCalibration != null))
            {
                this.IsCalibration(this.ComBuf_R, this.ComDataLen);
            }
            if (this.ComIsComplete != null)
            {
                this.ComIsComplete(this.ComCmd, this.ComBuf_R, this.ComDataLen);
            }
        }

        public void Init(Window win)
        {
            IntPtr handle = new WindowInteropHelper(win).Handle;
            HwndSource source = PresentationSource.FromVisual(win) as HwndSource;
            source.AddHook(new HwndSourceHook(this.WndProc));
            this.usb.RegisterHandle(source.Handle);

            //ProductId
            string s = "2628";      //v3
            //string s = "1028";    //v1
            
            //VendorId
            string str2 = "1013";

            this.usb.OnDeviceRemoved += new EventHandler(this.usb_OnDeviceRemoved);
            this.usb.OnSpecifiedDeviceArrived += new EventHandler(this.usb_OnSpecifiedDeviceArrived);
            this.usb.OnDeviceArrived += new EventHandler(this.usb_OnDeviceArrived);
            this.usb.OnDataRecieved += new DataRecievedEventHandler(this.usb_OnDataRecieved);
            this.usb.OnDataSend += new EventHandler(this.usb_OnDataSend);
            this.LinkF = false;
            this.usb.ProductId = int.Parse(s, NumberStyles.HexNumber);
            this.usb.VendorId = int.Parse(str2, NumberStyles.HexNumber);
            this.usb.CheckDevicePresent();
            this.UsbBuf_R = new byte[0x400];
            this.UsbBuf_T = new byte[0x400];
        }

        public virtual bool SenderPackToUsart(ref byte[] databuf, byte Cmd, int wLen)
        {
            byte[] destinationArray = new byte[wLen + 4];
            destinationArray[0] = 0;
            destinationArray[1] = (byte) (wLen & 0xff);
            destinationArray[2] = (byte) ((wLen >> 8) & 0xff);
            destinationArray[3] = Cmd;
            Array.Copy(databuf, 0, destinationArray, 4, wLen);
            this.Usb_PackSend(this.UCmd_ToUsart, ref destinationArray, destinationArray.Length);
            return true;
        }

        public virtual bool SenderPackToUsart(ref byte[] databuf, byte Cmd, int wLen, byte RetCmd, int WaitTime)
        {
            this.SenderPackToUsart(ref databuf, Cmd, wLen);
            this.AckCmd = RetCmd;
            this.IsAckCmd = false;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedMilliseconds < WaitTime)
            {
                if (this.IsAckCmd)
                {
                    return true;
                }
            }
            stopwatch.Stop();
            return false;
        }

        public void ThreadDoSend(object sender, DoWorkEventArgs e)
        {
            try
            {
                byte[] destinationArray = new byte[0x40];
                for (int i = this.UsbTxLen - this.UsbTxCnt; i > 0; i = this.UsbTxLen - this.UsbTxCnt)
                {
                    if (i > 0x40)
                    {
                        i = 0x40;
                    }
                    Array.Copy(this.UsbBuf_T, this.UsbTxCnt, destinationArray, 0, i);
                    this.UsbTxCnt += i;
                    if ((i > 0) && (this.usb.SpecifiedDevice != null))
                    {
                        this.usb.SpecifiedDevice.SendData(destinationArray);
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void usb_OnDataRecieved(object sender, DataRecievedEventArgs args)
        {
            byte[] array = new byte[0x40];
            args.data.CopyTo(array, 0);
            if (this.usPrevFlag != array[1])
            {
                int destinationIndex = array[2] * 0x3d;
                Array.Copy(array, 3, this.UsbBuf_R, destinationIndex, 0x3d);
                this.usPrevFlag = array[1];
                if ((this.usPrevFlag & 0x80) == 0)
                {
                    if (this.ComBuf_R == null)
                    {
                        this.ComBuf_R = new byte[0x400];
                    }
                    this.ComDataLen = (this.UsbBuf_R[2] << 8) | this.UsbBuf_R[1];
                    this.ComDataLen -= 4;
                    this.ComCmd = this.UsbBuf_R[3];
                    Array.Copy(this.UsbBuf_R, 4, this.ComBuf_R, 0, this.ComDataLen);
                    if (this.ComCmd == this.AckCmd)
                    {
                        this.IsAckCmd = true;
                    }
                    this.ComComplete();
                }
                this.UsbRCount += 0x40;
            }
        }

        private void usb_OnDataSend(object sender, EventArgs e)
        {
            this.UsbTCount += 0x40;
        }

        private void usb_OnDeviceArrived(object sender, EventArgs e)
        {
        }

        private void usb_OnDeviceRemoved(object sender, EventArgs e)
        {
            this.LinkF = false;
        }

        private void usb_OnSpecifiedDeviceArrived(object sender, EventArgs e)
        {
            this.LinkF = true;
        }

        public void Usb_PackSend(byte uCmd, ref byte[] databuf, int wLen)
        {
            this.UsbTxLen = 0;
            this.UsbBuf_T[this.UsbTxLen++] = 1;
            this.UsbBuf_T[this.UsbTxLen++] = (byte) (this.UsbsFlag | 0x80);
            this.UsbsFlag = (byte) (this.UsbsFlag + 1);
            this.UsbBuf_T[this.UsbTxLen++] = 0;
            this.UsbBuf_T[this.UsbTxLen++] = 0;
            wLen += 4;
            this.UsbBuf_T[this.UsbTxLen++] = (byte) (wLen & 0xff);
            this.UsbBuf_T[this.UsbTxLen++] = (byte) ((wLen >> 8) & 0xff);
            wLen -= 4;
            this.UsbBuf_T[this.UsbTxLen++] = uCmd;
            int index = 0;
            while (this.UsbTxLen < 0x40)
            {
                if (index < databuf.Length)
                {
                    this.UsbBuf_T[this.UsbTxLen] = databuf[index];
                }
                this.UsbTxLen++;
                index++;
            }
            int num3 = 1;
            while (index < wLen)
            {
                this.UsbBuf_T[this.UsbTxLen++] = 1;
                this.UsbBuf_T[this.UsbTxLen++] = (byte) (this.UsbsFlag | 0x80);
                this.UsbsFlag = (byte) (this.UsbsFlag + 1);
                this.UsbBuf_T[this.UsbTxLen++] = (byte) num3++;
                for (int i = 0; i < 0x3d; i++)
                {
                    if (index < databuf.Length)
                    {
                        this.UsbBuf_T[this.UsbTxLen] = databuf[index++];
                    }
                    this.UsbTxLen++;
                }
            }
            this.UsbBuf_T[this.UsbTxLen - 0x3f] = (byte) (this.UsbBuf_T[this.UsbTxLen - 0x3f] & 0x7f);
            this.UsbTxCnt = 0;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(this.ThreadDoSend);
            worker.RunWorkerAsync();
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            this.usb.ParseMessages(msg, wParam);
            return IntPtr.Zero;
        }

        public delegate void Event_ReadEEPRom(byte[] databuf, int datalen);

        public delegate void Event_RecDebugData(byte[] databuf, int datalen);

        public delegate void Event_RecDisString(string s, int cmd);

        public delegate void Evnet_ComComplete(byte cmd, byte[] databuf, int dlen);
    }
}

