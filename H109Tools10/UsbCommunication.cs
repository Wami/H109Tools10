namespace H109Tools10
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Threading;

    public class UsbCommunication : HidUsbCommunication
    {
        public byte[] CRC8_Table = new byte[] { 
            0, 0x5e, 0xbc, 0xe2, 0x61, 0x3f, 0xdd, 0x83, 0xc2, 0x9c, 0x7e, 0x20, 0xa3, 0xfd, 0x1f, 0x41,
            0x9d, 0xc3, 0x21, 0x7f, 0xfc, 0xa2, 0x40, 30, 0x5f, 1, 0xe3, 0xbd, 0x3e, 0x60, 130, 220,
            0x23, 0x7d, 0x9f, 0xc1, 0x42, 0x1c, 0xfe, 160, 0xe1, 0xbf, 0x5d, 3, 0x80, 0xde, 60, 0x62,
            190, 0xe0, 2, 0x5c, 0xdf, 0x81, 0x63, 0x3d, 0x7c, 0x22, 0xc0, 0x9e, 0x1d, 0x43, 0xa1, 0xff,
            70, 0x18, 250, 0xa4, 0x27, 0x79, 0x9b, 0xc5, 0x84, 0xda, 0x38, 0x66, 0xe5, 0xbb, 0x59, 7,
            0xdb, 0x85, 0x67, 0x39, 0xba, 0xe4, 6, 0x58, 0x19, 0x47, 0xa5, 0xfb, 120, 0x26, 0xc4, 0x9a,
            0x65, 0x3b, 0xd9, 0x87, 4, 90, 0xb8, 230, 0xa7, 0xf9, 0x1b, 0x45, 0xc6, 0x98, 0x7a, 0x24,
            0xf8, 0xa6, 0x44, 0x1a, 0x99, 0xc7, 0x25, 0x7b, 0x3a, 100, 0x86, 0xd8, 0x5b, 5, 0xe7, 0xb9,
            140, 210, 0x30, 110, 0xed, 0xb3, 0x51, 15, 0x4e, 0x10, 0xf2, 0xac, 0x2f, 0x71, 0x93, 0xcd,
            0x11, 0x4f, 0xad, 0xf3, 0x70, 0x2e, 0xcc, 0x92, 0xd3, 0x8d, 0x6f, 0x31, 0xb2, 0xec, 14, 80,
            0xaf, 0xf1, 0x13, 0x4d, 0xce, 0x90, 0x72, 0x2c, 0x6d, 0x33, 0xd1, 0x8f, 12, 0x52, 0xb0, 0xee,
            50, 0x6c, 0x8e, 0xd0, 0x53, 13, 0xef, 0xb1, 240, 0xae, 0x4c, 0x12, 0x91, 0xcf, 0x2d, 0x73,
            0xca, 0x94, 0x76, 40, 0xab, 0xf5, 0x17, 0x49, 8, 0x56, 180, 0xea, 0x69, 0x37, 0xd5, 0x8b,
            0x57, 9, 0xeb, 0xb5, 0x36, 0x68, 0x8a, 0xd4, 0x95, 0xcb, 0x29, 0x77, 0xf4, 170, 0x48, 0x16,
            0xe9, 0xb7, 0x55, 11, 0x88, 0xd6, 0x34, 0x6a, 0x2b, 0x75, 0x97, 0xc9, 0x4a, 20, 0xf6, 0xa8,
            0x74, 0x2a, 200, 150, 0x15, 0x4b, 0xa9, 0xf7, 0xb6, 0xe8, 10, 0x54, 0xd7, 0x89, 0x6b, 0x35
        };
        private short FrameLen;
        private IntPtr hDevice;
        private byte[] IoRxBuff;
        private int IoRxCnt;
        private byte[] IoTxBuff;
        private int IoTxWriteCnt;
        private byte PrevFrameCnt;
        private byte[] RecResult;
        private DispatcherTimer TimeCom;
        private byte TxFrameCnt;
        private byte UsbCommFlag = 0xa5;

        public void AddDataFrame(byte[] databuff, byte uCmd, int dlen)
        {
            if (this.IoTxBuff == null)
            {
                this.IoTxBuff = new byte[0x200];
            }
            byte[] bytes = BitConverter.GetBytes((short) (dlen + 1));
            this.IoTxBuff[this.IoTxWriteCnt] = this.UsbCommFlag;
            Array.Copy(bytes, 0, this.IoTxBuff, this.IoTxWriteCnt + 2, 2);
            this.IoTxBuff[this.IoTxWriteCnt + 4] = uCmd;
            Array.Copy(databuff, 0, this.IoTxBuff, this.IoTxWriteCnt + 5, dlen);
            this.IoTxBuff[this.IoTxWriteCnt + 1] = this.crc8(this.IoTxBuff, this.IoTxWriteCnt + 2, dlen + 3);
            this.IoTxWriteCnt += dlen + 5;
        }

        public int CheckRecBuff(int ReadLen)
        {
            if (this.IoRxBuff == null)
            {
                this.IoRxBuff = new byte[0x200];
                base.ComBuf_R = new byte[0x200];
                this.RecResult = new byte[0x200];
                this.IoRxCnt = 0;
            }
            if (this.hDevice == IntPtr.Zero)
            {
                this.FrameLen = 0;
                this.IoRxCnt = 0;
                return 0;
            }
            if (this.FrameLen == 0)
            {
                if (!DiskIoReadData(this.hDevice, Marshal.UnsafeAddrOfPinnedArrayElement(this.IoRxBuff, 0), ReadLen))
                {
                    return -1;
                }
                this.FrameLen = BitConverter.ToInt16(this.IoRxBuff, 1);
                if (this.PrevFrameCnt != this.IoRxBuff[0])
                {
                    this.PrevFrameCnt = this.IoRxBuff[0];
                }
                else
                {
                    this.FrameLen = 0;
                    this.IoRxCnt = 3;
                }
            }
            if (this.FrameLen != 0)
            {
                for (int i = this.IoRxCnt; i < this.FrameLen; i++)
                {
                    if (this.IoRxBuff[i] == this.UsbCommFlag)
                    {
                        short length = BitConverter.ToInt16(this.IoRxBuff, i + 2);
                        if ((this.crc8(this.IoRxBuff, i + 2, length + 2) == this.IoRxBuff[i + 1]) && (length > 0))
                        {
                            Array.Copy(this.IoRxBuff, i + 4, this.RecResult, 0, length);
                            this.IoRxCnt += length + 4;
                            if (this.IoRxCnt >= this.FrameLen)
                            {
                                this.FrameLen = 0;
                            }
                            return length;
                        }
                    }
                }
            }
            this.FrameLen = 0;
            this.IoRxCnt = 0;
            return 0;
        }

        private byte crc8(byte[] buffer, int offset, int len)
        {
            byte num2 = 0;
            if ((offset + len) >= buffer.Length)
            {
                return 0;
            }
            for (int i = 0; i < len; i++)
            {
                num2 = this.CRC8_Table[num2 ^ buffer[offset]];
                offset++;
            }
            return num2;
        }

        [DllImport("DeviceComm.dll", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
        public static extern void DiskIo_Close(IntPtr hDevice);
        [DllImport("DeviceComm.dll", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
        public static extern IntPtr DiskIo_Open(char diskname);
        [DllImport("DeviceComm.dll", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
        public static extern bool DiskIoReadData(IntPtr hDevice, IntPtr pdataout, int dlen);
        [DllImport("DeviceComm.dll", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
        public static extern bool DiskIoWriteData(IntPtr hDevice, IntPtr pdataout, int dlen);
        [DllImport("DeviceComm.dll", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
        public static extern bool GetDisksProperty(IntPtr hDevice, IntPtr poutinf);
        public void Open()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            byte[] arr = new byte[0x24];
            int index = -1;
            for (int i = 0; i < drives.Length; i++)
            {
                if (drives[i].DriveType == DriveType.Removable)
                {
                    IntPtr hDevice = DiskIo_Open(drives[i].Name[0]);
                    GetDisksProperty(hDevice, Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0));
                    DiskIo_Close(hDevice);
                    if (Encoding.Default.GetString(arr, 8, 11) == "HUBSAN")
                    {
                        index = i;
                        break;
                    }
                }
            }
            this.hDevice = IntPtr.Zero;
            if (index != -1)
            {
                this.hDevice = DiskIo_Open(drives[index].Name[0]);
            }
        }

        public int ReadData(int ReadLen) => 
            this.CheckRecBuff(ReadLen);

        private void timer_Tick_Com(object sender, EventArgs e)
        {
            int num;
            do
            {
                num = this.CheckRecBuff(0x200);
                if (num > 0)
                {
                    base.ComDataLen = num - 1;
                    if (base.ComDataLen < base.ComBuf_R.Length)
                    {
                        base.ComCmd = this.RecResult[0];
                        Array.Copy(this.RecResult, 1, base.ComBuf_R, 0, base.ComDataLen);
                        base.ComComplete();
                    }
                }
            }
            while (num > 0);
        }

        public bool WriteData(byte[] databuff, byte uCmd, int dlen)
        {
            byte num;
            if (this.hDevice == IntPtr.Zero)
            {
                this.IoTxWriteCnt = 3;
                this.Open();
                return false;
            }
            this.AddDataFrame(databuff, uCmd, dlen);
            this.TxFrameCnt = (byte) ((num = this.TxFrameCnt) + 1);
            this.IoTxBuff[0] = num;
            byte[] bytes = BitConverter.GetBytes((short) this.IoTxWriteCnt);
            this.IoTxBuff[1] = bytes[0];
            this.IoTxBuff[2] = bytes[1];
            if (!DiskIoWriteData(this.hDevice, Marshal.UnsafeAddrOfPinnedArrayElement(this.IoTxBuff, 0), this.IoTxWriteCnt))
            {
                DiskIo_Close(this.hDevice);
                this.Open();
            }
            this.IoTxWriteCnt = 3;
            return true;
        }
    }
}

