namespace H109Tools10
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public class SGlobalVariable
    {
        public static bool isDebug = false;

        //++not implemented
        public static byte CCodeRestIMU = 0x13;
        public static byte CCodeSetAccMiddle = 0x16;
        public static byte CCodeSetCompass = 0x17;
        public static byte CCodeSetGyroTD = 0x15;
        public static byte CCodeSetGyroZero = 20;
        public static byte CI_Debug_1 = 9;
        public static byte CI_Debug_String = 2;
        public static byte CI_DebugButton1 = 14;
        public static byte CI_GetRecData = 20;
        public static byte CI_RCVal = 0x10;
        public static byte CI_RecoverSys = 0x15;
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

        public static byte CI_Calibrate1 = 6;
        public static byte CI_GetAdcStr = 12;
        public static byte CI_GetMacDesc = 13;
        public static byte CI_SetSysPram1 = 10;
        public static byte CI_UpdataMC = 0xc7; //note difference with HidUsbCommunication.cs   CI_UpdataMC = 0xc5



        public static int EP_AccAngleLevel = 12;

        //location of stuff in EEPROM       
        public static int[] SysEEPRom = new int[100];
        public static int UserParameterLoc = 0;  //not changed anywhere
        public static int EP_AltitudeP = (UserParameterLoc + 13);
        public static int EP_ARPidX = (UserParameterLoc + 9);
        public static int EP_ARPidY = (UserParameterLoc + 10);
        public static int EP_ARPidZ = (UserParameterLoc + 11);
        public static int EP_ASPid = (UserParameterLoc + 12);
        public static int EP_BalancePid = (UserParameterLoc + 13);
        public static int EP_CAttitude = (UserParameterLoc + 0x23);
        public static int EP_CRPidX = (UserParameterLoc + 0x20);
        public static int EP_CRPidY = (UserParameterLoc + 0x21);
        public static int EP_CRPidZ = (UserParameterLoc + 0x22);
        public static int EP_GpsSpeedPid = (UserParameterLoc + 14);
        public static int EP_MotorMix = (UserParameterLoc + 15);
        public static int EP_RC8CHMapRev = (UserParameterLoc + 1);
        public static int EP_RcModeCtr = (UserParameterLoc + 3);


        public static string FlightDesString; //FW version consists of:
        // FV version: V HwNumber.Manufacturer.SwNumber
        public static uint HwNumber;
        public static uint Manufacturer;
        public static uint SwNumber;
        public static uint IdNumber;
        public static uint FlightTime;


        public static bool IsSenserView = false; //not implemented
        public static tRecordData RecordData;   //not implemented
        public static WriteToAircraft WriteParameterToAircraft; //not implemented here

        public static UsbCommunication mUsbCommunication = new UsbCommunication();

        public static byte ToolsVersion = 10; //used for communication
        public static byte UCmd_MS_Data = 4; //not used


        public static byte GetByte(int loc)
        {
            int index = loc / 4;
            return BitConverter.GetBytes(SysEEPRom[index])[loc % 4];
        }

        public static short GetWord(int loc)
        {
            int index = loc / 2;
            byte[] bytes = BitConverter.GetBytes(SysEEPRom[index]);
            return new short[] { BitConverter.ToInt16(bytes, 0), BitConverter.ToInt16(bytes, 2) }[loc % 2];
        }

        public static uint SetBitInInt32(uint src, int bitstr, int bitlen, uint val)
        {
            uint[] numArray = new uint[] { 
                0, 1, 3, 7, 15, 0x1f, 0x3f, 0x7f, 0xff, 0x1ff, 0x3ff, 0x7ff, 0xfff, 0x1fff, 0x3fff, 0x7fff,
                0xffff, 0x1ffff, 0x3ffff, 0x7ffff, 0xfffff, 0x1fffff, 0x3fffff, 0x7fffff, 0xffffff, 0x1ffffff, 0x3ffffff, 0x7ffffff, 0xfffffff, 0x1fffffff, 0x3fffffff, 0x7fffffff,
                uint.MaxValue
            };
            uint num = numArray[bitlen] << bitstr;
            num = ~num;
            val = val << bitstr;
            src &= num;
            src |= val;
            return src;
        }

        public static void SetByte(int loc, byte val)
        {
            int index = loc / 4;
            byte[] bytes = BitConverter.GetBytes(SysEEPRom[index]);
            bytes[loc % 4] = val;
            SysEEPRom[index] = BitConverter.ToInt32(bytes, 0);
        }

        public static void SetWord(int loc, short val)
        {
            int index = loc / 2;
            byte[] bytes = BitConverter.GetBytes(SysEEPRom[index]);
            short[] numArray = new short[] { BitConverter.ToInt16(bytes, 0), BitConverter.ToInt16(bytes, 2) };
            numArray[loc % 2] = val;
            SysEEPRom[index] = (((ushort) numArray[1]) << 0x10) | ((ushort) numArray[0]);
        }

        public static byte EP_AALP
        {
            get
            {
                return GetByte(0x74);
            }
            set
            {
                SetByte(0x74, value);
            }
        }

        public static sbyte EP_AccLevelX
        {
            get
            {
                return ((sbyte)GetByte(0x30));

            }
            set
            {
                SetByte(0x30, (byte) value);
            }
        }

        public static sbyte EP_AccLevelY
        {
            get
            {
                return ((sbyte)GetByte(0x31));
            }
            set
            {
                SetByte(0x31, (byte) value);
            }
        }

        public static int EP_AccMiddleX
        {
            get
            {
                return SysEEPRom[13];
            }
            set
            {
                SysEEPRom[13] = value;
            }
        }

        public static int EP_AccMiddleY
        {
            get
            {
                return SysEEPRom[14];
            }
            set
            {
                SysEEPRom[14] = value;
            }
        }

        public static int EP_AccMiddleZ
        {
            get
            {
                return SysEEPRom[15];
            }
            set
            {
                SysEEPRom[15] = value;
            }
        }

        public static int EP_AccScaleX
        {
            get
            {
                return SysEEPRom[0x10];
            }
            set
            {
                SysEEPRom[0x10] = value;
            }
        }

        public static int EP_AccScaleY
        {
            get
            {
                return SysEEPRom[0x11];
            }
            set
            {
                SysEEPRom[0x11] = value;
            }
        }

        public static int EP_AccScaleZ
        {
            get
            {
                return SysEEPRom[0x12];
            }
            set
            {
                SysEEPRom[0x12] = value;
            }
        }

        public static byte EP_AF_C1
        {
            get
            {
                return GetByte(100);
            }
            set
            {
                SetByte(100, value);
            }
        }

        public static byte EP_AF_C2
        {
            get
            {
                return GetByte(0x65);
            }
            set
            {
                SetByte(0x65, value);
            }
        }

        public static byte EP_AF_C3
        {
            get
            {
                return GetByte(0x66);
            }
            set
            {
                SetByte(0x66, value);
            }
        }

        public static byte EP_AF_C4
        {
            get
            {
                return GetByte(0x67);
            }
            set
            {
                SetByte(0x67, value);
            }
        }

        public static byte EP_AlarmVol
        {
            get
            {
                return GetByte(0x8f);
            }
            set
            {
                SetByte(0x8f, value);
            }
        }

        public static byte EP_AltitudeLimit
        {
            get
            {
                return GetByte(150);
            }
            set
            {
                SetByte(150, value);
            }
        }

        public static int EP_ARPidX_D
        {
            get {
                return ((SysEEPRom [EP_ARPidX] >> 20) & 0x3ff);
            }
            set
            {
                SysEEPRom[EP_ARPidX] = (int) SetBitInInt32((uint) SysEEPRom[EP_ARPidX], 20, 10, (uint) value);
            }
        }

        public static int EP_ARPidX_I
        {
            get
            {
                return ((SysEEPRom[EP_ARPidX] >> 10) & 0x3ff);
            }
            set
            {
                SysEEPRom[EP_ARPidX] = (int) SetBitInInt32((uint) SysEEPRom[EP_ARPidX], 10, 10, (uint) value);
            }
        }

        public static int EP_ARPidX_P
        {
            get
            {
                return (SysEEPRom[EP_ARPidX] & 0x3ff);
            }
            set
            {
                SysEEPRom[EP_ARPidX] = (int) SetBitInInt32((uint) SysEEPRom[EP_ARPidX], 0, 10, (uint) value);
            }
        }

        public static int EP_ARPidY_D
        {
            get
            {
                return ((SysEEPRom[EP_ARPidY] >> 20) & 0x3ff);
            }
            set
            {
                SysEEPRom[EP_ARPidY] = (int) SetBitInInt32((uint) SysEEPRom[EP_ARPidY], 20, 10, (uint) value);
            }
        }

        public static int EP_ARPidY_I
        {
            get
            {
                return ((SysEEPRom[EP_ARPidY] >> 10) & 0x3ff);
            }
            set
            {
                SysEEPRom[EP_ARPidY] = (int) SetBitInInt32((uint) SysEEPRom[EP_ARPidY], 10, 10, (uint) value);
            }
        }

        public static int EP_ARPidY_P
        {
            get
            {
                return (SysEEPRom[EP_ARPidY] & 0x3ff);
            }
            set
            {
                SysEEPRom[EP_ARPidY] = (int) SetBitInInt32((uint) SysEEPRom[EP_ARPidY], 0, 10, (uint) value);
            }
        }

        public static int EP_ARPidZ_D
        {
            get
            {
                return ((SysEEPRom[EP_ARPidZ] >> 20) & 0x3ff);
            }
            set
            {
                SysEEPRom[EP_ARPidZ] = (int) SetBitInInt32((uint) SysEEPRom[EP_ARPidZ], 20, 10, (uint) value);
            }
        }

        public static int EP_ARPidZ_I
        {
            get
            {
                return ((SysEEPRom[EP_ARPidZ] >> 10) & 0x3ff);
            }
            set
            {
                SysEEPRom[EP_ARPidZ] = (int) SetBitInInt32((uint) SysEEPRom[EP_ARPidZ], 10, 10, (uint) value);
            }
        }

        public static int EP_ARPidZ_P
        {
            get
            {
                return (SysEEPRom[EP_ARPidZ] & 0x3ff);
            }
            set
            {
                SysEEPRom[EP_ARPidZ] = (int) SetBitInInt32((uint) SysEEPRom[EP_ARPidZ], 0, 10, (uint) value);
            }
        }

        public static int EP_BalancePid_P
        {
            get
            {
                return (SysEEPRom[EP_BalancePid] & 0x3ff);
            }
            set
            {
                SysEEPRom[EP_BalancePid] = (SysEEPRom[EP_BalancePid] & -1024) | (value & 0x3ff);
            }
        }

        public static uint EP_DevKey
        {
            get
            {
                return ((uint)SysEEPRom[0x16]);
            }
            set
            {
                SysEEPRom[0x16] = (int) value;
            }
        }

        public static uint EP_FlightTime
        {
            get
            {
                return ((uint)SysEEPRom[0x17]);
            }
            set
            {
                SysEEPRom[0x17] = (int) value;
            }
        }

        public static byte EP_FType_FS
        {
            get
            {
                return GetByte(0x8e);
            }
            set
            {
                SetByte(0x8e, value);
            }
        }

        public static sbyte EP_GasBias
        {
            get {
                return ((sbyte) GetByte(0x22));
            }
            set
            {
                SetByte(0x22, (byte) value);
            }
        }

        public static byte EP_GpsCtrD
        {
            get {
                return GetByte(0x5e);
            }
            set
            {
                SetByte(0x5e, value);
            }
        }

        public static byte EP_GpsCtrI
        {
            get {
                return GetByte(0x5d);
            }
            set
            {
                SetByte(0x5d, value);
            }
        }

        public static byte EP_GpsCtrP
        {
            get {
                return GetByte(0x5c);
            }
            set
            {
                SetByte(0x5c, value);
            }
        }

        public static int EP_GpsSpeedPid_P
        {
            get {
                return (SysEEPRom[EP_GpsSpeedPid] & 0x3ff);
            }
            set
            {
                SysEEPRom[EP_GpsSpeedPid] = (SysEEPRom[EP_GpsSpeedPid] & -1024) | (value & 0x3ff);
            }
        }

        public static int EP_GyroBiasT
        {
            get {
                return SysEEPRom[4];
            }
            set
            {
                SysEEPRom[4] = value;
            }
        }

        public static int EP_GyroBiasX
        {
            get {
                return SysEEPRom[1];
            }
            set
            {
                SysEEPRom[1] = value;
            }
        }

        public static int EP_GyroBiasY
        {
            get {
                return SysEEPRom[2];
            }
            set
            {
                SysEEPRom[2] = value;
            }
        }

        public static int EP_GyroBiasZ
        {
            get {
                return SysEEPRom[3];
            }
            set
            {
                SysEEPRom[3] = value;
            }
        }

        public static int EP_GyroOrthZx
        {
            get {
                return SysEEPRom[5];
            }
            set
            {
                SysEEPRom[5] = value;
            }
        }

        public static int EP_GyroOrthZy
        {
            get {
                return SysEEPRom[6];
            }
            set
            {
                SysEEPRom[6] = value;
            }
        }

        public static int EP_GyroScaleX
        {
            get {
                return SysEEPRom[10];
            }
            set
            {
                SysEEPRom[10] = value;
            }
        }

        public static int EP_GyroScaleY
        {
            get {
                return SysEEPRom[11];
            }
            set
            {
                SysEEPRom[11] = value;
            }
        }

        public static int EP_GyroScaleZ
        {
            get {
                return SysEEPRom[12];
            }
            set
            {
                SysEEPRom[12] = value;
            }
        }

        public static int EP_GyroTempDriftX
        {
            get {
                return SysEEPRom[7];
            }
            set
            {
                SysEEPRom[7] = value;
            }
        }

        public static int EP_GyroTempDriftY
        {
            get {
                return SysEEPRom[8];
            }
            set
            {
                SysEEPRom[8] = value;
            }
        }

        public static int EP_GyroTempDriftZ
        {
            get {
                return SysEEPRom[9];
            }
            set
            {
                SysEEPRom[9] = value;
            }
        }

        public static byte EP_HAccelerateCtr
        {
            get {
                return GetByte(0x93);
            }
            set
            {
                SetByte(0x93, value);
            }
        }

        public static byte EP_HardwareEdition
        {
            get {
                return GetByte(0x61);
            }
            set
            {
                SetByte(0x61, value);
            }
        }

        public static byte EP_HSpeedCtr
        {
            get {
                return GetByte(0x92);
            }
            set
            {
                SetByte(0x92, value);
            }
        }

        public static byte EP_LandingVol
        {
            get {
                return GetByte(0x90);
            }
            set
            {
                SetByte(0x90, value);
            }
        }

        public static int EP_MagMidX
        {
            get {
                return SysEEPRom[0x13];
            }
            set
            {
                SysEEPRom[0x13] = value;
            }
        }

        public static int EP_MagMidY
        {
            get {
                return SysEEPRom[20];
            }
            set
            {
                SysEEPRom[20] = value;
            }
        }

        public static int EP_MagMidZ
        {
            get {
                return SysEEPRom[0x15];
            }
            set
            {
                SysEEPRom[0x15] = value;
            }
        }

        public static byte EP_Manufacturer
        {
            get {
                return GetByte(0x60);
            }
            set
            {
                SetByte(0x60, value);
            }
        }

        public static byte EP_MotorOutBais
        {
            get {
                return GetByte(0x97);
            }
            set
            {
                SetByte(0x97, value);
            }
        }

        public static byte EP_MoveAccLmt
        {
            get {
                return GetByte(0x8d);
            }
            set
            {
                SetByte(0x8d, value);
            }
        }

        public static byte EP_NavMaxSpeed
        {
            get {
                return GetByte(0x91);
            }
            set
            {
                SetByte(0x91, value);
            }
        }

        public static sbyte EP_PCameraCtr
        {
            get {
                return ((sbyte) GetByte(0x80));
            }
            set
            {
                SetByte(0x80, (byte) value);
            }
        }

        public static byte EP_PCameraMax
        {
            get {
                return GetByte(0x83);
            }
            set
            {
                SetByte(0x83, value);
            }
        }

        public static byte EP_PCameraMid
        {
            get {
                return GetByte(130);
            }
            set
            {
                SetByte(130, value);
            }
        }

        public static byte EP_PCameraMin
        {
            get {
                return GetByte(0x81);
            }
            set
            {
                SetByte(0x81, value);
            }
        }

        public static sbyte EP_PCDamper
        {
            get {
                return ((sbyte) GetByte(0x7c));
            }
            set
            {
                SetByte(0x7c, (byte) value);
            }
        }

        public static byte EP_PressureCtrP
        {
            get {
                return GetByte(140);
            }
            set
            {
                SetByte(140, value);
            }
        }

        public static byte EP_RadiusLimit
        {
            get {
                return GetByte(0x95);
            }
            set
            {
                SetByte(0x95, value);
            }
        }

        public static sbyte EP_RC_CHMiddle_P
        {
            get {
                return ((sbyte) GetByte(0));
            }
            set
            {
                SetByte(0, (byte) value);
            }
        }

        public static sbyte EP_RC_CHMiddle_R
        {
            get {
                return ((sbyte) GetByte(1));
            }
            set
            {
                SetByte(1, (byte) value);
            }
        }

        public static sbyte EP_RC_CHMiddle_Y
        {
            get {
                return ((sbyte) GetByte(2));
            }
            set
            {
                SetByte(2, (byte) value);
            }
        }

        public static short EP_RC_ThrMax
        {
            get {
                return ((byte) GetWord(5));
            }
            set
            {
                SetWord(5, (byte) value);
            }
        }

        public static short EP_RC_ThrMin
        {
            get {
                return GetWord(4);
            }
            set
            {
                SetWord(4, (byte) value);
            }
        }

        public static sbyte EP_RCameraCtr
        {
            get {
                return ((sbyte) GetByte(0x84));
            }
            set
            {
                SetByte(0x84, (byte) value);
            }
        }

        public static byte EP_RCameraMax
        {
            get {
                return GetByte(0x87);
            }
            set
            {
                SetByte(0x87, value);
            }
        }

        public static byte EP_RCameraMid
        {
            get {
                return GetByte(0x86);
            }
            set
            {
                SetByte(0x86, value);
            }
        }

        public static byte EP_RCameraMin
        {
            get {
                return GetByte(0x85);
            }
            set
            {
                SetByte(0x85, value);
            }
        }

        public static sbyte EP_RCDamper
        {
            get {
                return ((sbyte) GetByte(0x7d));
            }
            set
            {
                SetByte(0x7d, (byte) value);
            }
        }

        public static byte EP_SafeAltitude
        {
            get {
                return GetByte(0x94);
            }
            set
            {
                SetByte(0x94, value);
            }
        }

        public static byte EP_XYSF_C1
        {
            get {
                return GetByte(0x68);
            }
            set
            {
                SetByte(0x68, value);
            }
        }

        public static byte EP_XYSF_C2
        {
            get {
                return GetByte(0x69);
            }
            set
            {
                SetByte(0x69, value);
            }
        }

        public static byte EP_XYSF_C3
        {
            get {
                return GetByte(0x6a);
            }
            set
            {
                SetByte(0x6a, value);
            }
        }

        public static byte EP_XYSF_C4
        {
            get {
                return GetByte(0x6b);
            }
            set
            {
                SetByte(0x6b, value);
            }
        }

        public static sbyte EP_YCameraCtr
        {
            get {
                return ((sbyte) GetByte(0x88));
            }
            set
            {
                SetByte(0x88, (byte) value);
            }
        }

        public static byte EP_YCameraMax
        {
            get {
                return GetByte(0x8b);
            }
            set
            {
                SetByte(0x8b, value);
            }
        }

        public static byte EP_YCameraMid
        {
            get {
                return GetByte(0x8a);
            }
            set
            {
                SetByte(0x8a, value);
            }
        }

        public static byte EP_YCameraMin
        {
            get {
                return GetByte(0x89);
            }
            set
            {
                SetByte(0x89, value);
            }
        }

        public static sbyte EP_YCDamper
        {
            get {
                return ((sbyte) GetByte(0x7e));
            }
            set
            {
                SetByte(0x7e, (byte) value);
            }
        }

        public static byte EP_ZSF_C1
        {
            get {
                return GetByte(0x6c);
            }
            set
            {
                SetByte(0x6c, value);
            }
        }

        public static byte EP_ZSF_C2
        {
            get {
                return GetByte(0x6d);
            }
            set
            {
                SetByte(0x6d, value);
            }
        }

        public static byte EP_ZSF_C3
        {
            get {
                return GetByte(110);
            }
            set
            {
                SetByte(110, value);
            }
        }

        public static byte EP_ZSF_C4
        {
            get {
                return GetByte(0x6f);
            }
            set
            {
                SetByte(0x6f, value);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct tRecordData
        {
            public uint GpsDateTime;
            public float fGyroX;
            public float fGyroY;
            public float fGyroZ;
            public float fAccX;
            public float fAccY;
            public float fAccZ;
            public float fMagX;
            public float fMagY;
            public float fMagZ;
            public float fAttX;
            public float fAttY;
            public float fAttZ;
            public float fMovX;
            public float fMovY;
            public float fMovZ;
            private float fAccelerateX;
            private float fAccelerateY;
            private float fAccelerateZ;
            public int GpsX;
            public int GpsY;
            public float fAlttiude;
            public short GpsSpeed;
            public short GpsCoursem;
            public short GpsHDOP;
            public short GpsAltitude;
            public byte BatterVolatge;
            public byte FlightMode;
            public byte FlightState;
            public byte GpsStateNum;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=8)]
            public short[] Motor;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=20)]
            public short[] RcInput;
            public short MotorThrottleThrottle;
        }

        public delegate bool WriteToAircraft();
    }
}

