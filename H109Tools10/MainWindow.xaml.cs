using Microsoft.Win32;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.IO;

namespace H109Tools10
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //private bool _contentLoaded;

        private string BinFilePatch;
        private bool iswrite;       
        private bool linkstate_prev;
        private bool StopUpgrade;
        private bool isConnected=false;

        public MainWindow()
        {
            this.InitializeComponent();
            base.Loaded += new RoutedEventHandler(this.MainWindow_Loaded);
        }
        //--

        //++
        private DoubleAnimation AnimationDoShow(double From, double to, double ar, double dr, double duration)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = new double?(From),
                To = new double?(to),
                Duration = TimeSpan.FromMilliseconds(duration),
                AccelerationRatio = ar,
                DecelerationRatio = dr,
                FillBehavior = FillBehavior.HoldEnd
            };
            animation.Completed += new EventHandler(this.doubleAnimation_Completed);
            return animation;
        }
        //--

        private void AutoHide_Label(Label l1, string msg)
        {
            double duration = 2000.0;
            if (!this.iswrite)
            {
                l1.Content = "parameter reading is complete.";
            }
            else
            {
                l1.Content = "save parameter is complete.";
            }
            l1.BeginAnimation(UIElement.OpacityProperty, this.AnimationDoShow(1.0, 0.0, 0.1, 1.0 / duration, duration), HandoffBehavior.SnapshotAndReplace);
        }


        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Update_button.IsEnabled = true;
        }


        public void ComSendPack(ref byte[] databuf, byte Cmd, int wLen)
        {
            SGlobalVariable.mUsbCommunication.SenderPackToUsart(ref databuf, Cmd, wLen);
        }

        private void doubleAnimation_Completed(object sender, EventArgs e)
        {
        }
        
        // some actions on USB execution end
        public void Evnet_IsComComplete(byte cmd, byte[] databuf, int datalen)
        {
            if (base.Dispatcher.Thread != Thread.CurrentThread)
            {
                base.Dispatcher.Invoke(new HidUsbCommunication.Evnet_ComComplete(this.Evnet_IsComComplete), new object[] { cmd, databuf, datalen });
            }
            else if (cmd == HidUsbCommunication.CI_SetSysPram1) //set or read UAV system params
            {
                for (int i = 0; i < (datalen / 4); i++)
                {
                    SGlobalVariable.SysEEPRom[i] = BitConverter.ToInt32(databuf, i * 4);
                }
                if (!this.iswrite)
                {
                    this.AutoHide_Label(this.LContent, "parameter reading is complete.");
                }
                else
                {
                    this.AutoHide_Label(this.LContent, "save parameter is complete.");
                }
                this.iswrite = false;
                this.ShowParameter();
            }
            else if ((cmd != HidUsbCommunication.CI_SetSysPram2) && (cmd == SGlobalVariable.CI_GetMacDesc))
            {
                this.fwinf.Content = SGlobalVariable.FlightDesString;
            }
        }

        // FW update routine
        // Reads file, decodes it, flashes, updates progress on button.
        public void MainControlUpgrade(object sender, DoWorkEventArgs e)
        {
            byte[] databuf = new byte[600];
            int block_size_const = 0x100;
            this.StopUpgrade = false;
            byte[] f_data = File.ReadAllBytes(this.BinFilePatch);
            if (f_data == null)
            {
                MessageBox.Show((string)base.FindResource("ContentUpdateErr"), "Firmware", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else
            {
                databuf[0] = 0;
                databuf[1] = 1;
                databuf[2] = 0;
                databuf[3] = 1;
                SGlobalVariable.mUsbCommunication.SenderPackToUsart(ref databuf, HidUsbCommunication.CI_UpdataMC, 0x10);
                Thread.Sleep(100);

                // useless block?
                int offset_1 = f_data[20] + f_data[30];
                offset_1 *= f_data[21];
                offset_1 += f_data[29];
                int file_len_1 = offset_1;
                file_len_1 *= f_data[0x1c];
                // end of useless block?

                offset_1 = f_data[1] + (~f_data[3] * 0x100);
                file_len_1 = (f_data[offset_1 + 5] + (f_data[offset_1 + 7] << 8)) + (f_data[offset_1 + 9] << 0x10);

                if (file_len_1 > 0x1fc020)
                {
                    MessageBox.Show("File lengh error");
                }
                else
                {
                    ushort checksum_1 = f_data[offset_1 + 12];
                    checksum_1 = (ushort)(checksum_1 | ((ushort)(f_data[offset_1 + 13] << 8)));

                    byte[] mask = new byte[10];
                    mask[0] = f_data[offset_1 + 14];
                    mask[1] = f_data[offset_1 + 15];
                    mask[2] = f_data[offset_1 + 16];
                    mask[3] = f_data[offset_1 + 17];
                    mask[4] = f_data[offset_1 + 18];
                    mask[5] = f_data[offset_1 + 19];
                    mask[6] = f_data[offset_1 + 20];

                    byte[] decoded_data_1 = new byte[file_len_1];

                    int index = 0;
                    int mask_iter = 0;
                    int num12 = (offset_1 + 4) + 0x20;

                    while (index < file_len_1)
                    {
                        byte num16 = f_data[num12++];
                        byte num15 = f_data[num12++];

                        if ((index % 4) == 0)
                        {
                            decoded_data_1[index] = (byte)((num16 & 240) >> 4);
                            decoded_data_1[index] = (byte)(decoded_data_1[index] | ((byte)((num15 & 15) << 4)));
                            decoded_data_1[index] = (byte)(decoded_data_1[index] ^ mask[mask_iter]);
                        }
                        else if ((index % 4) == 1)
                        {
                            decoded_data_1[index] = (byte)((num16 & 240) >> 4);
                            decoded_data_1[index] = (byte)(decoded_data_1[index] | ((byte)(num15 & 240)));
                            decoded_data_1[index] = (byte)(decoded_data_1[index] ^ mask[mask_iter]);
                        }
                        else if ((index % 4) == 2)
                        {
                            decoded_data_1[index] = (byte)(num16 & 15);
                            decoded_data_1[index] = (byte)(decoded_data_1[index] | ((byte)((num15 & 15) << 4)));
                            decoded_data_1[index] = (byte)(decoded_data_1[index] ^ mask[mask_iter]);
                        }
                        else if ((index % 4) == 3)
                        {
                            decoded_data_1[index] = (byte)(num16 & 15);
                            decoded_data_1[index] = (byte)(decoded_data_1[index] | ((byte)(num15 & 240)));
                            decoded_data_1[index] = (byte)(decoded_data_1[index] ^ mask[mask_iter]);
                        }
                        if (++mask_iter >= 7)
                        {
                            mask_iter = 0;
                        }
                        index++;
                    }

                    byte[] decoded_data_final = new byte[file_len_1];
                    for (index = 0; index < file_len_1; index++)
                    {
                        decoded_data_final[index] = (byte)~decoded_data_1[index];
                    }

                    index = 0;
                    ushort calc_checksum_1 = 0;
                    while (index < file_len_1)
                    {
                        calc_checksum_1 = (ushort)(calc_checksum_1 + decoded_data_final[index++]);
                    }
                    if (calc_checksum_1 != checksum_1)
                    {
                        MessageBox.Show("checksum error");
                    }
                    else
                    {
                        int num4;
                        byte[] buffer3 = new byte[file_len_1];
                        int total_byta_read = 0;
                        while ((total_byta_read < decoded_data_final.Length) && !this.StopUpgrade)
                        {
                            int block_size = decoded_data_final.Length - total_byta_read;
                            if (block_size > block_size_const)
                            {
                                block_size = block_size_const; //if left more than 0x100 then block_size=0x100 else block_size=left_size
                            }

                            databuf[0] = (byte)(total_byta_read & 0xff);
                            databuf[1] = (byte)((total_byta_read >> 8) & 0xff); //2 bytes index 
                            databuf[2] = (byte)(block_size & 0xff);
                            databuf[3] = (byte)((block_size >> 8) & 0xff);// 2 bytes block size


                            byte block_checksum = 0;
                            int block_index = 0;
                            while (block_index < block_size)
                            {
                                block_checksum = (byte)(block_checksum + decoded_data_final[total_byta_read]);
                                databuf[block_index + 4] = decoded_data_final[total_byta_read++];
                                block_index++;
                            }
                            databuf[block_index + 4] = block_checksum;
                            block_index++;
                            int index_2 = 0;
                            int index_3 = 0;
                            bool flag = true;
                            do //make 3 attemts to send block
                            {
                                SGlobalVariable.mUsbCommunication.UpdateRecData = null;
                                SGlobalVariable.mUsbCommunication.SenderPackToUsart(ref databuf, HidUsbCommunication.CI_UpdataMC, block_size_const + 4); //send block to usb.CI_UpdataMC
                                for (index_3 = 0; (index_3 < 100) && (SGlobalVariable.mUsbCommunication.UpdateRecData == null); index_3++) //sleep
                                {
                                    Thread.Sleep(10);
                                }
                                if (SGlobalVariable.mUsbCommunication.UpdateRecData != null)
                                {
                                    flag = false;
                                    int num19 = total_byta_read - block_size; //the first byte of the sent data
                                    num4 = 0;
                                    while (num4 < block_size)
                                    {
                                        if (decoded_data_final[num19] != SGlobalVariable.mUsbCommunication.UpdateRecData[num4 + 4])
                                        {
                                            flag = true;
                                        }
                                        buffer3[num19++] = SGlobalVariable.mUsbCommunication.UpdateRecData[num4 + 4];
                                        num4++;
                                    }
                                }
                                index_2++;
                            }
                            while ((index_2 < 3) && flag);


                            if (index_2 == 3)
                            {
                                MessageBox.Show("update error", "Firmware", MessageBoxButton.OK, MessageBoxImage.Hand);
                                return;
                            }
                            double num5 = ((double)total_byta_read) / ((double)decoded_data_final.Length);
                            string txt = ((num5 * 100.0)).ToString("f1") + "%";
                            this.setbutton(this.Update_button, txt);
                        }
                        if (!this.StopUpgrade)
                        {
                            for (num4 = 0; num4 < buffer3.Length; num4++)
                            {
                                if (buffer3[num4] != decoded_data_final[num4])
                                {
                                    MessageBox.Show(((string)base.FindResource("ContentUpdateErr")) + num4.ToString(), "Firmware", MessageBoxButton.OK, MessageBoxImage.Hand);
                                    num4 = buffer3.Length + 4;
                                    return;
                                }
                            }
                            databuf[0] = 0xff;
                            databuf[1] = 0xff;
                            databuf[2] = 0xff;
                            databuf[3] = 0xff;
                            SGlobalVariable.mUsbCommunication.SenderPackToUsart(ref databuf, HidUsbCommunication.CI_UpdataMC, 4);
                        }
                    }
                }
            }
        }
        
        // On window load
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // connection initialization
            SGlobalVariable.mUsbCommunication.Init(this);
            SGlobalVariable.mUsbCommunication.ComIsComplete += new HidUsbCommunication.Evnet_ComComplete(this.Evnet_IsComComplete);
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 100),
                IsEnabled = true
            };
            timer.Tick += new EventHandler(this.timer_Tick);
        }

        private void setbutton(Button obj, string txt)
        {
            if (obj.Dispatcher.Thread != Thread.CurrentThread)
            {
                obj.Dispatcher.Invoke(new _setbutton(this.setbutton), new object[] { obj, txt });
            }
            else
            {
                obj.Content = txt;
            }
        }

        public void ShowParameter()
        {
            int num1 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_ARPidX];
            int num2 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_ARPidX];
            int num3 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_ARPidX];
            int num4 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_ARPidY];
            int num5 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_ARPidY];
            int num6 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_ARPidY];
            int num7 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_ARPidZ];
            int num8 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_ARPidZ];
            int num9 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_ARPidZ];
            int num10 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_BalancePid];
            int num11 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_BalancePid];
            int num12 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_BalancePid];
            int num13 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_ASPid];
            int num14 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_ASPid];
            int num15 = SGlobalVariable.SysEEPRom[SGlobalVariable.EP_ASPid];
        }

        public void ShowAllParameter()
        {
            string[] paramStr = new string[85];
            paramStr[0] = "AALP             GetByte(0x74): " + SGlobalVariable.EP_AALP.ToString();
            paramStr[1] = "AccLevelX (sbyte)GetByte(0x30): " + SGlobalVariable.EP_AccLevelX.ToString();
            paramStr[2] = "AccLevelY (sbyte)GetByte(0x31): " + SGlobalVariable.EP_AccLevelY.ToString();

            paramStr[3] = "AccMiddleX          EEPRom[13]: " + SGlobalVariable.EP_AccMiddleX.ToString();
            paramStr[4] = "AccMiddleY          EEPRom[14]: " + SGlobalVariable.EP_AccMiddleY.ToString();
            paramStr[5] = "AccMiddleZ          EEPRom[15]: " + SGlobalVariable.EP_AccMiddleZ.ToString();

            paramStr[6] = "AccScaleX         EEPRom[0x10]: " + SGlobalVariable.EP_AccScaleX.ToString();
            paramStr[7] = "AccScaleY         EEPRom[0x11]: " + SGlobalVariable.EP_AccScaleY.ToString();
            paramStr[8] = "AccScaleZ         EEPRom[0x12]: " + SGlobalVariable.EP_AccScaleZ.ToString();

            paramStr[9] = "AF_C1             GetByte(100): " + SGlobalVariable.EP_AF_C1.ToString();
            paramStr[10] = "AF_C2            GetByte(0x65): " + SGlobalVariable.EP_AF_C2.ToString();
            paramStr[11] = "AF_C3            GetByte(0x66): " + SGlobalVariable.EP_AF_C3.ToString();
            paramStr[12] = "AF_C4            GetByte(0x67): " + SGlobalVariable.EP_AF_C4.ToString();

            paramStr[13] = "AlarmVol         GetByte(0x8f): " + SGlobalVariable.EP_AlarmVol.ToString();
            paramStr[14] = "AltitudeLimit     GetByte(150): " + SGlobalVariable.EP_AltitudeLimit.ToString();

            paramStr[15] = "ARPidX_D EEPRom [EP_ARPidX] >> 20) & 0x3ff: " + SGlobalVariable.EP_ARPidX_D.ToString();
            paramStr[16] = "ARPidX_I EEPRom [EP_ARPidX] >> 10) & 0x3ff: " + SGlobalVariable.EP_ARPidX_I.ToString();
            paramStr[17] = "ARPidX_P EEPRom [EP_ARPidX] >> 00) & 0x3ff: " + SGlobalVariable.EP_ARPidX_P.ToString();

            paramStr[18] = "ARPidY_D EEPRom [EP_ARPidY] >> 20) & 0x3ff: " + SGlobalVariable.EP_ARPidY_D.ToString();
            paramStr[19] = "ARPidY_I EEPRom [EP_ARPidY] >> 10) & 0x3ff: " + SGlobalVariable.EP_ARPidY_I.ToString();
            paramStr[20] = "ARPidY_P EEPRom [EP_ARPidY] >> 00) & 0x3ff: " + SGlobalVariable.EP_ARPidY_P.ToString();

            paramStr[21] = "ARPidZ_D EEPRom [EP_ARPidZ] >> 20) & 0x3ff: " + SGlobalVariable.EP_ARPidZ_D.ToString();
            paramStr[22] = "ARPidZ_I EEPRom [EP_ARPidZ] >> 10) & 0x3ff: " + SGlobalVariable.EP_ARPidZ_I.ToString();
            paramStr[23] = "ARPidZ_P EEPRom [EP_ARPidZ] >> 00) & 0x3ff: " + SGlobalVariable.EP_ARPidZ_P.ToString();

            paramStr[24] = "BalancePid_P EEPRom[EP_BalancePid] & 0x3ff): " + SGlobalVariable.EP_BalancePid_P.ToString();

            paramStr[25] = "DevKey            EEPRom[0x16]: " + SGlobalVariable.EP_DevKey.ToString();
            paramStr[26] = "FlightTime        EEPRom[0x17]: " + SGlobalVariable.EP_FlightTime.ToString();
            paramStr[27] = "FType_FS         GetByte(0x8e): " + SGlobalVariable.EP_FType_FS.ToString();

            paramStr[28] = "GpsCtrD          GetByte(0x5e): " + SGlobalVariable.EP_GpsCtrD.ToString();
            paramStr[29] = "GpsCtrI          GetByte(0x5d): " + SGlobalVariable.EP_GpsCtrI.ToString();
            paramStr[30] = "GpsCtrP          GetByte(0x5c): " + SGlobalVariable.EP_GpsCtrP.ToString();

            paramStr[31] = "GpsSpeedPid_P SysEEPRom[EP_GpsSpeedPid] & 0x3ff: " + SGlobalVariable.EP_GpsSpeedPid_P.ToString();
            paramStr[32] = "GyroBiasT         SysEEPRom[4]: " + SGlobalVariable.EP_GyroBiasT.ToString();
            paramStr[33] = "GyroBiasX         SysEEPRom[1]: " + SGlobalVariable.EP_GyroBiasX.ToString();
            paramStr[34] = "GyroBiasZ         SysEEPRom[3]: " + SGlobalVariable.EP_GyroBiasZ.ToString();
            paramStr[35] = "GyroOrthZx        SysEEPRom[5]: " + SGlobalVariable.EP_GyroOrthZx.ToString();
            paramStr[36] = "GyroOrthZy        SysEEPRom[6]: " + SGlobalVariable.EP_GyroOrthZy.ToString();
            paramStr[37] = "GyroScaleX        SysEEPRom[10]: " + SGlobalVariable.EP_GyroScaleX.ToString();
            paramStr[38] = "GyroScaleY        SysEEPRom[11]: " + SGlobalVariable.EP_GyroScaleY.ToString();
            paramStr[39] = "GyroScaleZ        SysEEPRom[12]: " + SGlobalVariable.EP_GyroScaleZ.ToString();

            paramStr[40] = "GyroTempDriftX     SysEEPRom[7]: " + SGlobalVariable.EP_GyroTempDriftX.ToString();
            paramStr[41] = "GyroTempDriftY     SysEEPRom[8]: " + SGlobalVariable.EP_GyroTempDriftY.ToString();
            paramStr[42] = "GyroTempDriftZ     SysEEPRom[9]: " + SGlobalVariable.EP_GyroTempDriftZ.ToString();

            paramStr[43] = "HAccelerateCtr    GetByte(0x93): " + SGlobalVariable.EP_HAccelerateCtr.ToString();
            paramStr[44] = "HardwareEdition   GetByte(0x61): " + SGlobalVariable.EP_HardwareEdition.ToString();
            paramStr[45] = "HSpeedCtr         GetByte(0x92): " + SGlobalVariable.EP_HSpeedCtr.ToString();
            paramStr[46] = "LandingVol        GetByte(0x90): " + SGlobalVariable.EP_LandingVol.ToString();
            paramStr[47] = "MagMidX            EEPRom[0x13]: " + SGlobalVariable.EP_MagMidX.ToString();
            paramStr[48] = "MagMidY            EEPRom[0x14]: " + SGlobalVariable.EP_MagMidY.ToString();
            paramStr[49] = "MagMidZ            EEPRom[0x15]: " + SGlobalVariable.EP_MagMidZ.ToString();
            paramStr[50] = "Manufacturer      GetByte(0x60): " + SGlobalVariable.EP_Manufacturer.ToString();
            paramStr[51] = "MotorOutBais      GetByte(0x97): " + SGlobalVariable.EP_MotorOutBais.ToString();

            paramStr[52] = "MoveAccLmt       GetByte(0x8d): " + SGlobalVariable.EP_MoveAccLmt.ToString();
            paramStr[53] = "NavMaxSpeed      GetByte(0x91): " + SGlobalVariable.EP_NavMaxSpeed.ToString();

            paramStr[54] = "PCDamper (sbyte) GetByte(0x7c): " + SGlobalVariable.EP_PCDamper.ToString();
            paramStr[55] = "PressureCtrP      GetByte(140): " + SGlobalVariable.EP_PressureCtrP.ToString();
            paramStr[56] = "RadiusLimit      GetByte(0x95): " + SGlobalVariable.EP_RadiusLimit.ToString();

            paramStr[57] = "RC_CHMiddle_P (sbyte) GetByte(0): " + SGlobalVariable.EP_RC_CHMiddle_P.ToString();
            paramStr[58] = "RC_CHMiddle_R (sbyte) GetByte(1): " + SGlobalVariable.EP_RC_CHMiddle_R.ToString();
            paramStr[59] = "RC_CHMiddle_Y (sbyte) GetByte(2): " + SGlobalVariable.EP_RC_CHMiddle_Y.ToString();
            paramStr[60] = "RC_ThrMax     (byte) GetWord(5): " + SGlobalVariable.EP_RC_ThrMax.ToString();
            paramStr[61] = "RC_ThrMin            GetWord(4): " + SGlobalVariable.EP_RC_ThrMin.ToString();


            paramStr[62] = "RCDamper   (sbyte) GetByte(0x7d): " + SGlobalVariable.EP_RCDamper.ToString();
            paramStr[63] = "SafeAltitude       GetByte(0x94): " + SGlobalVariable.EP_SafeAltitude.ToString();

            paramStr[64] = "XYSF_C1            GetByte(0x68): " + SGlobalVariable.EP_XYSF_C1.ToString();
            paramStr[65] = "XYSF_C2            GetByte(0x69): " + SGlobalVariable.EP_XYSF_C2.ToString();
            paramStr[66] = "XYSF_C3            GetByte(0x6a): " + SGlobalVariable.EP_XYSF_C3.ToString();
            paramStr[67] = "XYSF_C4            GetByte(0x6b): " + SGlobalVariable.EP_XYSF_C4.ToString();

            paramStr[68] = "ZSF_C1            GetByte(0x6c): " + SGlobalVariable.EP_ZSF_C1.ToString();
            paramStr[69] = "ZSF_C2            GetByte(0x6d): " + SGlobalVariable.EP_ZSF_C2.ToString();
            paramStr[70] = "ZSF_C3            GetByte(0x6e): " + SGlobalVariable.EP_ZSF_C3.ToString();
            paramStr[71] = "ZSF_C4            GetByte(0x6f): " + SGlobalVariable.EP_ZSF_C4.ToString();

            paramStr[72] = "PCameraCtr (sbyte) GetByte(0x80): " + SGlobalVariable.EP_PCameraCtr.ToString();
            paramStr[73] = "PCameraMax         GetByte(0x83): " + SGlobalVariable.EP_PCameraMax.ToString();
            paramStr[74] = "PCameraMid         GetByte(0x82): " + SGlobalVariable.EP_PCameraMid.ToString();
            paramStr[75] = "PCameraMin         GetByte(0x81): " + SGlobalVariable.EP_PCameraMin.ToString();

            paramStr[76] = "RCameraCtr (sbyte) GetByte(0x84): " + SGlobalVariable.EP_RCameraCtr.ToString();
            paramStr[77] = "RCameraMax         GetByte(0x87): " + SGlobalVariable.EP_RCameraMax.ToString();
            paramStr[78] = "RCameraMid         GetByte(0x86): " + SGlobalVariable.EP_RCameraMid.ToString();
            paramStr[79] = "RCameraMin         GetByte(0x85): " + SGlobalVariable.EP_RCameraMin.ToString();

            paramStr[80] = "YCameraCtr (sbyte) GetByte(0x88): " + SGlobalVariable.EP_YCameraCtr.ToString();
            paramStr[81] = "YCameraMax         GetByte(0x8b): " + SGlobalVariable.EP_YCameraMax.ToString();
            paramStr[82] = "YCameraMid         GetByte(0x8a): " + SGlobalVariable.EP_YCameraMid.ToString();
            paramStr[83] = "YCameraMin         GetByte(0x89): " + SGlobalVariable.EP_YCameraMin.ToString();

            paramStr[84] = "YCDamper   (sbyte) GetByte(0x7e): " + SGlobalVariable.EP_YCDamper.ToString();

            string outStr = "";
            foreach (string s in paramStr)
            {
                outStr = outStr + "/n";
            }


        }


        public void UpdateParameterTable()
        {
            uint[] numArray = new uint[0];
            SGlobalVariable.EP_ARPidX_P = (int)numArray[0];
            SGlobalVariable.EP_ARPidY_P = (int)numArray[1];
            SGlobalVariable.EP_ARPidZ_P = (int)numArray[2];
            SGlobalVariable.EP_SafeAltitude = (byte)numArray[3];
            SGlobalVariable.EP_NavMaxSpeed = (byte)numArray[4];
            SGlobalVariable.EP_AltitudeLimit = (byte)numArray[5];
            SGlobalVariable.EP_RadiusLimit = (byte)numArray[6];
            SGlobalVariable.EP_AlarmVol = (byte)numArray[7];
        }


        //Action on UAV connection
        private void timer_Tick(object sender, EventArgs e)
        {
            if (!SGlobalVariable.mUsbCommunication.LinkF)
            {
                this.LContent.Content = "not connect";
                this.isConnected = false;
            }
            else
            {
                this.LContent.Content = "connected";
                this.isConnected = true;

                if (File.Exists(this.BinFilePatch))
                {
                    this.Update_button.IsEnabled = true;
                }
            }
            this.linkstate_prev = SGlobalVariable.mUsbCommunication.LinkF;
        }


        //send system parameters to UAV
        public bool WriteParameterToAircraft()
        {
            byte[] destinationArray = new byte[(SGlobalVariable.SysEEPRom.Length * 4) + 1];
            destinationArray[0] = (byte)(SGlobalVariable.ToolsVersion | 0x80);
            for (int i = 0; i < SGlobalVariable.SysEEPRom.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(SGlobalVariable.SysEEPRom[i]), 0, destinationArray, (i * 4) + 1, 4);
            }
            return SGlobalVariable.mUsbCommunication.SenderPackToUsart(ref destinationArray, HidUsbCommunication.CI_SetSysPram1, destinationArray.Length, HidUsbCommunication.CI_SetSysPram1, 0x3e8);
        }

        private delegate void _setbutton(Button obj, string txt);

        private delegate void _setlabel(Label obj, string txt);

        private delegate void _setProgreebar(ProgressBar obj, double val);

        //Click on Open File button
        private void B_MC_Update_SFile_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "hbs file|*.hbs"
            };
            if (dialog.ShowDialog() == true)
            {
                ((Button)sender).Content = dialog.FileName;
                this.BinFilePatch = dialog.FileName.ToString();

                if (this.isConnected)
                    this.Update_button.IsEnabled = true;
            }
        }

        //Click on update FW button
        private void Update_button_Click(object sender, RoutedEventArgs e)
        {
            
            if (!File.Exists(this.BinFilePatch))
            {
                MessageBox.Show("File not exist");
                this.Update_button.IsEnabled = false;
            }
            else
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += new DoWorkEventHandler(this.MainControlUpgrade);
                worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
                worker.RunWorkerAsync();
                this.Update_button.IsEnabled = false;
            }
        }

        //Click on read FW info from UAV
        private void Refresh_FW_Inf_Click(object sender, RoutedEventArgs e)
        {
            byte[] databuf = new byte[1];
            SGlobalVariable.mUsbCommunication.SenderPackToUsart(ref databuf, HidUsbCommunication.CI_GetMacDesc, 0);
        }

        //NOT IMPLEMENTED  Click on read parameters from UAV
        public void Button_Click_RP(object sender, RoutedEventArgs e)
        {
            byte[] databuf = new byte[] { SGlobalVariable.ToolsVersion };
            SGlobalVariable.mUsbCommunication.SenderPackToUsart(ref databuf, HidUsbCommunication.CI_SetSysPram1, 1);
        }

        //NOT IMPLEMENTED  Click on send parameters to UAV
        public void Button_Click_WP(object sender, RoutedEventArgs e)
        {
            this.UpdateParameterTable();
            this.WriteParameterToAircraft();
        }
    }
}
