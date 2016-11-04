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
    /// 



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
                    //this.AutoHide_Label(this.LContent, "parameter reading is complete.");
                }
                else
                {
                    //this.AutoHide_Label(this.LContent, "save parameter is complete.");
                }
            
               this.iswrite = false;
               this.UpdateContent();
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

                offset_1 = f_data[1] + ((byte)~f_data[3] * 0x100);
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

                            SGlobalVariable.updated = true;

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

            this.AltLimitD.Visibility = Visibility.Hidden;
            this.AltLimitN.Visibility = Visibility.Hidden;
            this.AltLimitP.Visibility = Visibility.Hidden;

            this.RadiusLimitN.Visibility = Visibility.Hidden;
            this.RadiusLimitD.Visibility = Visibility.Hidden;
            this.RadiusLimitP.Visibility = Visibility.Hidden;

            this.ReturnAltitudeN.Visibility = Visibility.Hidden;
            this.ReturnAltitudeD.Visibility = Visibility.Hidden;
            this.ReturnAltitudeP.Visibility = Visibility.Hidden;

            this.NavMaxSpeedN.Visibility = Visibility.Hidden;
            this.NavMaxSpeedD.Visibility = Visibility.Hidden;
            this.NavMaxSpeedP.Visibility = Visibility.Hidden;

            this.AlarmVolN.Visibility = Visibility.Hidden;
            this.AlarmVolD.Visibility = Visibility.Hidden;
            this.AlarmVolP.Visibility = Visibility.Hidden;

            this.LandingVolN.Visibility = Visibility.Hidden;
            this.LandingVolD.Visibility = Visibility.Hidden;
            this.LandingVolP.Visibility = Visibility.Hidden;

            //debug code
            if (SGlobalVariable.isDebug)
            {
                string eepromFileName = ".\\H501S.eeprom.hbs";
                if (File.Exists(eepromFileName))
                {

                    byte[] f_data = File.ReadAllBytes(eepromFileName);
                    int datalen = f_data.Length;

                    for (int i = 0; i < (datalen / 4); i++)
                    {
                        SGlobalVariable.SysEEPRom[i] = BitConverter.ToInt32(f_data, i * 4);
                    }
                }
            }
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


        public void UpdateContent()
        {
            

                string[] paramStr = new string[] {
                "AlarmVol         GetByte(0x8f): " + SGlobalVariable.EP_AlarmVol.ToString(),
                "LandingVol       GetByte(0x90): " + SGlobalVariable.EP_LandingVol.ToString(),
                "",

                "RadiusLimit      GetByte(0x95): " + SGlobalVariable.EP_RadiusLimit.ToString(),
                "AltitudeLimit     GetByte(150): " + SGlobalVariable.EP_AltitudeLimit.ToString(),
                "SafeAltitude     GetByte(0x94): " + SGlobalVariable.EP_SafeAltitude.ToString(),
                "NavMaxSpeed      GetByte(0x91): " + SGlobalVariable.EP_NavMaxSpeed.ToString(),
                "",

                "MoveAccLmt       GetByte(0x8d): " + SGlobalVariable.EP_MoveAccLmt.ToString(),
                "HSpeedCtr        GetByte(0x92): " + SGlobalVariable.EP_HSpeedCtr.ToString(),
                "HAccelerateCtr   GetByte(0x93): " + SGlobalVariable.EP_HAccelerateCtr.ToString(),
                "MotorOutBais     GetByte(0x97): " + SGlobalVariable.EP_MotorOutBais.ToString(),
                "FlightTime        EEPRom[0x17]: " + SGlobalVariable.EP_FlightTime.ToString(),
                "Manufacturer     GetByte(0x60): " + SGlobalVariable.EP_Manufacturer.ToString(),
                "HardwareEdition  GetByte(0x61): " + SGlobalVariable.EP_HardwareEdition.ToString(),
                "DevKey            EEPRom[0x16]: " + SGlobalVariable.EP_DevKey.ToString(),
                "FType_FS         GetByte(0x8e): " + SGlobalVariable.EP_FType_FS.ToString(),
                "",

                "AALP             GetByte(0x74): " + SGlobalVariable.EP_AALP.ToString(),
                "AccLevelX (sbyte)GetByte(0x30): " + SGlobalVariable.EP_AccLevelX.ToString(),
                "AccLevelY (sbyte)GetByte(0x31): " + SGlobalVariable.EP_AccLevelY.ToString(),

                "AccMiddleX          EEPRom[13]: " + SGlobalVariable.EP_AccMiddleX.ToString(),
                "AccMiddleY          EEPRom[14]: " + SGlobalVariable.EP_AccMiddleY.ToString(),
                "AccMiddleZ          EEPRom[15]: " + SGlobalVariable.EP_AccMiddleZ.ToString(),

                "AccScaleX         EEPRom[0x10]: " + SGlobalVariable.EP_AccScaleX.ToString(),
                "AccScaleY         EEPRom[0x11]: " + SGlobalVariable.EP_AccScaleY.ToString(),
                "AccScaleZ         EEPRom[0x12]: " + SGlobalVariable.EP_AccScaleZ.ToString(),

                "ARPidX_D EEPRom[EP_ARPidX]>>20)&0x3ff: " + SGlobalVariable.EP_ARPidX_D.ToString(),
                "ARPidX_I EEPRom[EP_ARPidX]>>10)&0x3ff: " + SGlobalVariable.EP_ARPidX_I.ToString(),
                "ARPidX_P EEPRom[EP_ARPidX]>>00)&0x3ff: " + SGlobalVariable.EP_ARPidX_P.ToString(),

                "ARPidY_D EEPRom[EP_ARPidY]>>20)&0x3ff: " + SGlobalVariable.EP_ARPidY_D.ToString(),
                "ARPidY_I EEPRom[EP_ARPidY]>>10)&0x3ff: " + SGlobalVariable.EP_ARPidY_I.ToString(),
                "ARPidY_P EEPRom[EP_ARPidY]>>00)&0x3ff: " + SGlobalVariable.EP_ARPidY_P.ToString(),

                "ARPidZ_D EEPRom[EP_ARPidZ]>>20)&0x3ff: " + SGlobalVariable.EP_ARPidZ_D.ToString(),
                "ARPidZ_I EEPRom[EP_ARPidZ]>>10)&0x3ff: " + SGlobalVariable.EP_ARPidZ_I.ToString(),
                "ARPidZ_P EEPRom[EP_ARPidZ]>>00)&0x3ff: " + SGlobalVariable.EP_ARPidZ_P.ToString(),

                "BalancePid_P EEPRom[EP_BalancePid] & 0x3ff): " + SGlobalVariable.EP_BalancePid_P.ToString(),

                "GpsCtrD           GetByte(0x5e): " + SGlobalVariable.EP_GpsCtrD.ToString(),
                "GpsCtrI           GetByte(0x5d): " + SGlobalVariable.EP_GpsCtrI.ToString(),
                "GpsCtrP           GetByte(0x5c): " + SGlobalVariable.EP_GpsCtrP.ToString(),

                "GpsSpeedPid_P SysEEPRom[EP_GpsSpeedPid] & 0x3ff: " + SGlobalVariable.EP_GpsSpeedPid_P.ToString(),
                "GyroBiasT          SysEEPRom[4]: " + SGlobalVariable.EP_GyroBiasT.ToString(),
                "GyroBiasX          SysEEPRom[1]: " + SGlobalVariable.EP_GyroBiasX.ToString(),
                "GyroBiasZ          SysEEPRom[3]: " + SGlobalVariable.EP_GyroBiasZ.ToString(),
                "GyroOrthZx         SysEEPRom[5]: " + SGlobalVariable.EP_GyroOrthZx.ToString(),
                "GyroOrthZy         SysEEPRom[6]: " + SGlobalVariable.EP_GyroOrthZy.ToString(),
                "GyroScaleX        SysEEPRom[10]: " + SGlobalVariable.EP_GyroScaleX.ToString(),
                "GyroScaleY        SysEEPRom[11]: " + SGlobalVariable.EP_GyroScaleY.ToString(),
                "GyroScaleZ        SysEEPRom[12]: " + SGlobalVariable.EP_GyroScaleZ.ToString(),

                "GyroTempDriftX     SysEEPRom[7]: " + SGlobalVariable.EP_GyroTempDriftX.ToString(),
                "GyroTempDriftY     SysEEPRom[8]: " + SGlobalVariable.EP_GyroTempDriftY.ToString(),
                "GyroTempDriftZ     SysEEPRom[9]: " + SGlobalVariable.EP_GyroTempDriftZ.ToString(),

                "MagMidX            EEPRom[0x13]: " + SGlobalVariable.EP_MagMidX.ToString(),
                "MagMidY            EEPRom[0x14]: " + SGlobalVariable.EP_MagMidY.ToString(),
                "MagMidZ            EEPRom[0x15]: " + SGlobalVariable.EP_MagMidZ.ToString(),

                "RCDamper   (sbyte) GetByte(0x7d): " + SGlobalVariable.EP_RCDamper.ToString(),
                "PCDamper   (sbyte) GetByte(0x7c): " + SGlobalVariable.EP_PCDamper.ToString(),
                "YCDamper   (sbyte) GetByte(0x7e): " + SGlobalVariable.EP_YCDamper.ToString(),
                "PressureCtrP        GetByte(140): " + SGlobalVariable.EP_PressureCtrP.ToString(),

                "RC_CHMiddle_P (sbyte) GetByte(0): " + SGlobalVariable.EP_RC_CHMiddle_P.ToString(),
                "RC_CHMiddle_R (sbyte) GetByte(1): " + SGlobalVariable.EP_RC_CHMiddle_R.ToString(),
                "RC_CHMiddle_Y (sbyte) GetByte(2): " + SGlobalVariable.EP_RC_CHMiddle_Y.ToString(),
                "RC_ThrMax      (byte) GetWord(5): " + SGlobalVariable.EP_RC_ThrMax.ToString(),
                "RC_ThrMin             GetWord(4): " + SGlobalVariable.EP_RC_ThrMin.ToString(),

                "XYSF_C1            GetByte(0x68): " + SGlobalVariable.EP_XYSF_C1.ToString(),
                "XYSF_C2            GetByte(0x69): " + SGlobalVariable.EP_XYSF_C2.ToString(),
                "XYSF_C3            GetByte(0x6a): " + SGlobalVariable.EP_XYSF_C3.ToString(),
                "XYSF_C4            GetByte(0x6b): " + SGlobalVariable.EP_XYSF_C4.ToString(),

                "ZSF_C1             GetByte(0x6c): " + SGlobalVariable.EP_ZSF_C1.ToString(),
                "ZSF_C2             GetByte(0x6d): " + SGlobalVariable.EP_ZSF_C2.ToString(),
                "ZSF_C3             GetByte(0x6e): " + SGlobalVariable.EP_ZSF_C3.ToString(),
                "ZSF_C4             GetByte(0x6f): " + SGlobalVariable.EP_ZSF_C4.ToString(),

                "AF_C1               GetByte(100): " + SGlobalVariable.EP_AF_C1.ToString(),
                "AF_C2              GetByte(0x65): " + SGlobalVariable.EP_AF_C2.ToString(),
                "AF_C3              GetByte(0x66): " + SGlobalVariable.EP_AF_C3.ToString(),
                "AF_C4              GetByte(0x67): " + SGlobalVariable.EP_AF_C4.ToString(),

                "PCameraCtr (sbyte) GetByte(0x80): " + SGlobalVariable.EP_PCameraCtr.ToString(),
                "PCameraMax         GetByte(0x83): " + SGlobalVariable.EP_PCameraMax.ToString(),
                "PCameraMid         GetByte(0x82): " + SGlobalVariable.EP_PCameraMid.ToString(),
                "PCameraMin         GetByte(0x81): " + SGlobalVariable.EP_PCameraMin.ToString(),

                "RCameraCtr (sbyte) GetByte(0x84): " + SGlobalVariable.EP_RCameraCtr.ToString(),
                "RCameraMax         GetByte(0x87): " + SGlobalVariable.EP_RCameraMax.ToString(),
                "RCameraMid         GetByte(0x86): " + SGlobalVariable.EP_RCameraMid.ToString(),
                "RCameraMin         GetByte(0x85): " + SGlobalVariable.EP_RCameraMin.ToString(),

                "YCameraCtr (sbyte) GetByte(0x88): " + SGlobalVariable.EP_YCameraCtr.ToString(),
                "YCameraMax         GetByte(0x8b): " + SGlobalVariable.EP_YCameraMax.ToString(),
                "YCameraMid         GetByte(0x8a): " + SGlobalVariable.EP_YCameraMid.ToString(),
                "YCameraMin         GetByte(0x89): " + SGlobalVariable.EP_YCameraMin.ToString(),
                };

             

            this.listBox.Items.Clear();
            foreach (string s in paramStr)
            {
                //outStr = outStr + s + "\n";
                this.listBox.Items.Add(s);
            }

            this.AltLimitP.Text = SGlobalVariable.EP_AltitudeLimit.ToString();
            this.RadiusLimitP.Text = SGlobalVariable.EP_RadiusLimit.ToString();
            this.ReturnAltitudeP.Text = SGlobalVariable.EP_SafeAltitude.ToString();
            this.NavMaxSpeedP.Text = SGlobalVariable.EP_NavMaxSpeed.ToString();
            this.AlarmVolP.Text = SGlobalVariable.EP_AlarmVol.ToString();
            this.LandingVolP.Text = SGlobalVariable.EP_LandingVol.ToString();


            if(SGlobalVariable.show_all)
            {
                this.listBox.Visibility = Visibility.Visible;
                //SetParamVisibility(Visibility.Visible);
                SGlobalVariable.show_all = false;

                this.Show_all_params.Content = "Hide all params";

                this.Edit_params.IsEnabled = false;

            }
            else if (SGlobalVariable.show_edit)
             {
                SetParamVisibility(Visibility.Visible);
                SGlobalVariable.show_edit = false;


                this.Flash_params.Visibility = Visibility.Visible;

                this.Edit_params.Content = "Close edit params";


                this.Show_all_params.IsEnabled = false;

            }

        }



        private void SetParamVisibility(Visibility v)
        {
            this.AltLimitD.Visibility = v;
            this.AltLimitN.Visibility = v;
            this.AltLimitP.Visibility = v;

            this.RadiusLimitN.Visibility = v;
            this.RadiusLimitD.Visibility = v;
            this.RadiusLimitP.Visibility = v;

            this.ReturnAltitudeN.Visibility = v;
            this.ReturnAltitudeD.Visibility = v;
            this.ReturnAltitudeP.Visibility = v;

            this.NavMaxSpeedN.Visibility = v;
            this.NavMaxSpeedD.Visibility = v;
            this.NavMaxSpeedP.Visibility = v;

            this.AlarmVolN.Visibility = v;
            this.AlarmVolD.Visibility = v;
            this.AlarmVolP.Visibility = v;

            this.LandingVolN.Visibility = v;
            this.LandingVolD.Visibility = v;
            this.LandingVolP.Visibility = v;

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

                if (SGlobalVariable.updated && !this.Refresh_FW_Inf.IsEnabled)
                {
                    this.Refresh_FW_Inf.IsEnabled = true;
                    this.Show_all_params.IsEnabled = true;
                    this.Edit_params.IsEnabled = true;
                }
                if (SGlobalVariable.edit_mode)
                {
                  
                    this.Show_all_params.IsEnabled = true;
                    this.Edit_params.IsEnabled = true;
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

        //Click on Show_all_params
        private void Show_all_params_Click(object sender, RoutedEventArgs e)
        {
            if(!SGlobalVariable.isDebug) //if not debug get data from UAV
            {
                byte[] databuf = new byte[] { SGlobalVariable.ToolsVersion };
                SGlobalVariable.mUsbCommunication.SenderPackToUsart(ref databuf, HidUsbCommunication.CI_SetSysPram1, 1);

            }

            if (this.listBox.Visibility != Visibility.Visible)
            {
                SGlobalVariable.show_all = true;


            }
            else
            {
                this.listBox.Visibility = Visibility.Collapsed;
                this.Show_all_params.Content = "Show all params";

                this.Edit_params.IsEnabled = true;
            }
        }

        //Click on Edit Params
        private void Edit_params_Click(object sender, RoutedEventArgs e)
        {

            if (this.Flash_params.Visibility != Visibility.Visible)
            {
                
                if (!SGlobalVariable.isDebug) //if not debug get data from UAV
                {
                    byte[] databuf = new byte[] { SGlobalVariable.ToolsVersion };
                    SGlobalVariable.mUsbCommunication.SenderPackToUsart(ref databuf, HidUsbCommunication.CI_SetSysPram1, 1);

                }

                SGlobalVariable.show_edit = true;


            }
            else
            {
                this.Flash_params.Visibility = Visibility.Hidden;
                this.Edit_params.Content = "Edit params";

                SetParamVisibility(Visibility.Hidden);

                this.Show_all_params.IsEnabled = true;
            }
            
        }

        //Click on send parameters to UAV
        private void Flash_params_Click(object sender, RoutedEventArgs e)
        {
            bool haveErr = false;

            int val;
            if (int.TryParse(this.AltLimitP.Text, out val))
            {
                if (val > 255 || val < 0)
                    { val = 0; }
                SGlobalVariable.EP_AltitudeLimit = (byte)val;
            }
            else
            {
                MessageBox.Show("Error parsing Alt Limit");
                haveErr = true;
            }

            if (int.TryParse(this.RadiusLimitP.Text, out val))
            {
                if (val > 255 || val < 0)
                { val = 0; }
                SGlobalVariable.EP_RadiusLimit = (byte)val;
            }
            else
            {
                MessageBox.Show("Error parsing Radius Limit");
                haveErr = true;
            }

            if (int.TryParse(this.ReturnAltitudeP.Text, out val))
            {
                if (val > 255 || val < 0)
                { val = 0; }
                SGlobalVariable.EP_SafeAltitude = (byte)val;
            }
            else
            {
                MessageBox.Show("Error parsing Return Altitude");
                haveErr = true;
            }

            if (int.TryParse(this.NavMaxSpeedP.Text, out val))
            {
                if (val > 255 || val < 0)
                { val = 0; }
                SGlobalVariable.EP_NavMaxSpeed = (byte)val;
            }
            else
            {
                MessageBox.Show("Error parsing Nav Max Speed");
                haveErr = true;
            }

            if (int.TryParse(this.AlarmVolP.Text, out val))
            {
                if (val > 255 || val < 0)
                { val = 0; }
                SGlobalVariable.EP_AlarmVol = (byte)val;
            }
            else
            {
                MessageBox.Show("Error parsing Alarm Volt");
                haveErr = true;
            }

            if (int.TryParse(this.LandingVolP.Text, out val))
            {
                if (val > 255 || val < 0)
                { val = 0; }
                SGlobalVariable.EP_LandingVol = (byte)val;
            }
            else
            {
                MessageBox.Show("Error parsing Landing Volt");
                haveErr = true;
            }


            if(!haveErr && !SGlobalVariable.isDebug)
            {
                this.WriteParameterToAircraft();
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            byte[] databuf = new byte[10];
            databuf[0] = 0xff;
            databuf[1] = 0xff;
            databuf[2] = 0xff;
            databuf[3] = 0xff;
            SGlobalVariable.mUsbCommunication.SenderPackToUsart(ref databuf, HidUsbCommunication.CI_UpdataMC, 4);

            SGlobalVariable.edit_mode = true;
        }
    }
}
