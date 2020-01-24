using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace NVRCsharpDemo
{
    public partial class test1 : Form
    {
        private bool m_bInitSDK = false;
        private bool m_bRecord = false;
        private uint iLastErr = 0;
        private Int32 m_lUserID = -1;
        private Int32 m_lRealHandle = -1;
        private string str1;
        private string str2;
        private Int32 i = 0;
        private Int32 m_lTree = 0;
        private string str;
        private long iSelIndex = 0;
        private uint dwAChanTotalNum = 0;
        private uint dwDChanTotalNum = 0;
        private Int32 m_lPort = -1;
        private IntPtr m_ptrRealHandle;
        private int[] iIPDevID = new int[96];
        private int[] iChannelNum = new int[96];
        private int panel = 1;

        private CHCNetSDK.REALDATACALLBACK RealData = null;
        public CHCNetSDK.NET_DVR_DEVICEINFO_V30 DeviceInfo;
        public CHCNetSDK.NET_DVR_IPPARACFG_V40 m_struIpParaCfgV40;
        public CHCNetSDK.NET_DVR_STREAM_MODE m_struStreamMode;
        public CHCNetSDK.NET_DVR_IPCHANINFO m_struChanInfo;
        public CHCNetSDK.NET_DVR_IPCHANINFO_V40 m_struChanInfoV40;
        private PlayCtrl.DECCBFUN m_fDisplayFun = null;
        public delegate void MyDebugInfo(string str);

       

        public test1()
        {
            InitializeComponent();
            m_bInitSDK = CHCNetSDK.NET_DVR_Init();
            if (m_bInitSDK == false)
            {
                MessageBox.Show("NET_DVR_Init error!");
                return;
            }
            else
            {
                //保存SDK日志 To save the SDK log
                CHCNetSDK.NET_DVR_SetLogToFile(3, "C:\\SdkLog\\", true);



                for (int i = 0; i < 64; i++)
                {
                    iIPDevID[i] = -1;
                    iChannelNum[i] = -1;
                }
            }
            if (m_lUserID < 0)
            {
                string DVRIPAddress = "192.168.25.155"; //设备IP地址或者域名 Device IP
                Int16 DVRPortNumber = 8000;//设备服务端口号 Device Port
                string DVRUserName = "visualizacion";//设备登录用户名 User name to login
                string DVRPassword = "sier2019*";//设备登录密码 Password to login
                m_lUserID = CHCNetSDK.NET_DVR_Login_V30(DVRIPAddress, DVRPortNumber, DVRUserName, DVRPassword, ref DeviceInfo);
                dwAChanTotalNum = (uint)DeviceInfo.byChanNum;
                dwDChanTotalNum = (uint)DeviceInfo.byIPChanNum + 256 * (uint)DeviceInfo.byHighDChanNum;
                if (dwDChanTotalNum > 0)
                {
                    InfoIPChannel();
                }
                else
                {
                    for (i = 0; i < dwAChanTotalNum; i++)
                    {
                        ListAnalogChannel(i + 1, 1);
                        iChannelNum[i] = i + (int)DeviceInfo.byStartChan;
                    }

                   
                    // MessageBox.Show("This device has no IP channel!");
                }
            }
        }
        public void InfoIPChannel()
        {
            uint dwSize = (uint)Marshal.SizeOf(m_struIpParaCfgV40);

            IntPtr ptrIpParaCfgV40 = Marshal.AllocHGlobal((Int32)dwSize);
            Marshal.StructureToPtr(m_struIpParaCfgV40, ptrIpParaCfgV40, false);

            uint dwReturn = 0;
            int iGroupNo = 0;  //该Demo仅获取第一组64个通道，如果设备IP通道大于64路，需要按组号0~i多次调用NET_DVR_GET_IPPARACFG_V40获取

            if (!CHCNetSDK.NET_DVR_GetDVRConfig(m_lUserID, CHCNetSDK.NET_DVR_GET_IPPARACFG_V40, iGroupNo, ptrIpParaCfgV40, dwSize, ref dwReturn))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                str = "NET_DVR_GET_IPPARACFG_V40 failed, error code= " + iLastErr;
                //获取IP资源配置信息失败，输出错误号 Failed to get configuration of IP channels and output the error code
              
            }
            else
            {
               

                m_struIpParaCfgV40 = (CHCNetSDK.NET_DVR_IPPARACFG_V40)Marshal.PtrToStructure(ptrIpParaCfgV40, typeof(CHCNetSDK.NET_DVR_IPPARACFG_V40));

                for (i = 0; i < dwAChanTotalNum; i++)
                {
                    ListAnalogChannel(i + 1, m_struIpParaCfgV40.byAnalogChanEnable[i]);
                    iChannelNum[i] = i + (int)DeviceInfo.byStartChan;
                }

                byte byStreamType = 0;
                uint iDChanNum = 64;

                if (dwDChanTotalNum < 64)
                {
                    iDChanNum = dwDChanTotalNum; //如果设备IP通道小于64路，按实际路数获取
                }

                for (i = 0; i < iDChanNum; i++)
                {
                    iChannelNum[i + dwAChanTotalNum] = i + (int)m_struIpParaCfgV40.dwStartDChan;
                    byStreamType = m_struIpParaCfgV40.struStreamMode[i].byGetStreamType;

                    dwSize = (uint)Marshal.SizeOf(m_struIpParaCfgV40.struStreamMode[i].uGetStream);
                    switch (byStreamType)
                    {
                        //目前NVR仅支持直接从设备取流 NVR supports only the mode: get stream from device directly
                        case 0:
                            IntPtr ptrChanInfo = Marshal.AllocHGlobal((Int32)dwSize);
                            Marshal.StructureToPtr(m_struIpParaCfgV40.struStreamMode[i].uGetStream, ptrChanInfo, false);
                            m_struChanInfo = (CHCNetSDK.NET_DVR_IPCHANINFO)Marshal.PtrToStructure(ptrChanInfo, typeof(CHCNetSDK.NET_DVR_IPCHANINFO));

                            //列出IP通道 List the IP channel
                            ListIPChannel(i + 1, m_struChanInfo.byEnable, m_struChanInfo.byIPID);
                            iIPDevID[i] = m_struChanInfo.byIPID + m_struChanInfo.byIPIDHigh * 256 - iGroupNo * 64 - 1;

                            Marshal.FreeHGlobal(ptrChanInfo);
                            break;
                        case 6:
                            IntPtr ptrChanInfoV40 = Marshal.AllocHGlobal((Int32)dwSize);
                            Marshal.StructureToPtr(m_struIpParaCfgV40.struStreamMode[i].uGetStream, ptrChanInfoV40, false);
                            m_struChanInfoV40 = (CHCNetSDK.NET_DVR_IPCHANINFO_V40)Marshal.PtrToStructure(ptrChanInfoV40, typeof(CHCNetSDK.NET_DVR_IPCHANINFO_V40));

                            //列出IP通道 List the IP channel
                            ListIPChannel(i + 1, m_struChanInfoV40.byEnable, m_struChanInfoV40.wIPID);
                            iIPDevID[i] = m_struChanInfoV40.wIPID - iGroupNo * 64 - 1;

                            Marshal.FreeHGlobal(ptrChanInfoV40);
                            break;
                        default:
                            break;
                    }
                }
            }
            Marshal.FreeHGlobal(ptrIpParaCfgV40);

        }
        public void ListIPChannel(Int32 iChanNo, byte byOnline, int byIPID)
        {
            str1 = String.Format("IPCamera {0}", iChanNo);
            m_lTree++;

            if (byIPID == 0)
            {
                str2 = "X"; //通道空闲，没有添加前端设备 the channel is idle                  
            }
            else
            {
                if (byOnline == 0)
                {
                    str2 = "offline"; //通道不在线 the channel is off-line
                }
                else
                    str2 = "online"; //通道在线 The channel is on-line
            }

            listViewIPChannel.Items.Add(new ListViewItem(new string[] { str1, str2 }));//将通道添加到列表中 add the channel to the list
        }
        public void ListAnalogChannel(Int32 iChanNo, byte byEnable)
        {
            str1 = String.Format("Camera {0}", iChanNo);
            m_lTree++;

            if (byEnable == 0)
            {
                str2 = "Disabled"; //通道已被禁用 This channel has been disabled               
            }
            else
            {
                str2 = "Enabled"; //通道处于启用状态 This channel has been enabled
            }

            listViewIPChannel.Items.Add(new ListViewItem(new string[] { str1, str2 }));//将通道添加到列表中 add the channel to the list
        }

        public void Preview()
        {
            if (m_lRealHandle < 0)
            {
                CHCNetSDK.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO();

                //lpPreviewInfo.hPlayWnd = RealPlayWnd.Handle;//预览窗口 live view window
                //if (panel == 1)
                //{
                //    //lpPreviewInfo.hPlayWnd = RealPlayWnd.Handle;
                //}
                //if (panel == 2)
                //{
                    lpPreviewInfo.hPlayWnd = pictureBox1.Handle;
                //}
                lpPreviewInfo.lChannel = iChannelNum[(int)iSelIndex];//预览的设备通道 the device channel number
                lpPreviewInfo.dwStreamType = 0;//码流类型：0-主码流，1-子码流，2-码流3，3-码流4，以此类推
                lpPreviewInfo.dwLinkMode = 0;//连接方式：0- TCP方式，1- UDP方式，2- 多播方式，3- RTP方式，4-RTP/RTSP，5-RSTP/HTTP 
                lpPreviewInfo.bBlocked = true; //0- 非阻塞取流，1- 阻塞取流
                lpPreviewInfo.dwDisplayBufNum = 15; //播放库显示缓冲区最大帧数

                IntPtr pUser = IntPtr.Zero;//用户数据 user data 

                                   //打开预览 Start live view 
                    m_lRealHandle = CHCNetSDK.NET_DVR_RealPlay_V40(m_lUserID, ref lpPreviewInfo, null/*RealData*/, pUser);
                
                
                if (m_lRealHandle < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "NET_DVR_RealPlay_V40 failed, error code= " + iLastErr; //预览失败，输出错误号 failed to start live view, and output the error code.
                    return;
                }
              
            }
           
            return;
        }
        private void listViewIPChannel_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listViewIPChannel_DoubleClick(object sender, EventArgs e)
        {
            Preview();

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            panel = 2;
        }
        private void pintar(int matriz)
        {
            int maxX, miX;
            int maxY, miY;
            int sepa = 6;
            int dY = 6;
            maxX = groupBox1.Width;
            maxY = groupBox1.Height;
            miX = (maxX - (matriz + 1) * sepa) / matriz;
            miY = (maxY - (matriz + 1) * sepa - dY) / matriz;
            pictureBox1.Visible = false;
            pictureBox2.Visible = false;
            pictureBox3.Visible = false;
            pictureBox4.Visible = false;
            pictureBox5.Visible = false;
            pictureBox6.Visible = false;
            pictureBox7.Visible = false;
            pictureBox8.Visible = false;
            pictureBox9.Visible = false;
            switch (matriz)
            {
                case 1:
                    pictureBox1.Location = new Point(sepa, sepa + dY);
                    break;
                case 2:
                    pictureBox1.Location = new Point(1 * sepa + 0 * miX, 1 * sepa + 0 * miY + dY);
                    pictureBox2.Location = new Point(2 * sepa + 1 * miX, 1 * sepa + 0 * miY + dY);
                    pictureBox3.Location = new Point(1 * sepa + 0 * miX, 2 * sepa + 1 * miY + dY);
                    pictureBox4.Location = new Point(2 * sepa + 1 * miX, 2 * sepa + 1 * miY + dY);
                    break;
                case 3:
                    pictureBox1.Location = new Point(1 * sepa + 0 * miX, 1 * sepa + 0 * miY + dY);
                    pictureBox2.Location = new Point(2 * sepa + 1 * miX, 1 * sepa + 0 * miY + dY);
                    pictureBox3.Location = new Point(3 * sepa + 2 * miX, 1 * sepa + 0 * miY + dY);
                    pictureBox4.Location = new Point(1 * sepa + 0 * miX, 2 * sepa + 1 * miY + dY);
                    pictureBox5.Location = new Point(2 * sepa + 1 * miX, 2 * sepa + 1 * miY + dY);
                    pictureBox6.Location = new Point(3 * sepa + 2 * miX, 2 * sepa + 1 * miY + dY);
                    pictureBox7.Location = new Point(1 * sepa + 0 * miX, 3 * sepa + 2 * miY + dY);
                    pictureBox8.Location = new Point(2 * sepa + 1 * miX, 3 * sepa + 2 * miY + dY);
                    pictureBox9.Location = new Point(3 * sepa + 2 * miX, 3 * sepa + 2 * miY + dY);
                    break;
            }
            pictureBox1.Width = miX;
            pictureBox2.Width = miX;
            pictureBox3.Width = miX;
            pictureBox4.Width = miX;
            pictureBox5.Width = miX;
            pictureBox6.Width = miX;
            pictureBox7.Width = miX;
            pictureBox8.Width = miX;
            pictureBox9.Width = miX;
            pictureBox1.Height = miY;
            pictureBox2.Height = miY;
            pictureBox3.Height = miY;
            pictureBox4.Height = miY;
            pictureBox5.Height = miY;
            pictureBox6.Height = miY;
            pictureBox7.Height = miY;
            pictureBox8.Height = miY;
            pictureBox9.Height = miY;
            pictureBox1.Visible = true;
            if (matriz >= 2)
            {
                pictureBox2.Visible = true;
                pictureBox3.Visible = true;
                pictureBox4.Visible = true;
            }
            if (matriz > 2)
            {
                pictureBox5.Visible = true;
                pictureBox6.Visible = true;
                pictureBox7.Visible = true;
                pictureBox8.Visible = true;
                pictureBox9.Visible = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pintar(1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            pintar(2);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            pintar(3);
        }

        private void button4_MouseDown(object sender, MouseEventArgs e)
        {
            bool test = CHCNetSDK.NET_DVR_PTZControl_Other(m_lUserID, iChannelNum[(int)iSelIndex], 21, 0);
        }

        private void button4_MouseUp(object sender, MouseEventArgs e)
        {
            bool test = CHCNetSDK.NET_DVR_PTZControl_Other(m_lUserID, iChannelNum[(int)iSelIndex], 21, 1);
        }

        private void button5_MouseDown(object sender, MouseEventArgs e)
        {
            bool test = CHCNetSDK.NET_DVR_PTZControl_Other(m_lUserID, iChannelNum[(int)iSelIndex], 22, 0);
        }

        private void button5_MouseUp(object sender, MouseEventArgs e)
        {
            bool test = CHCNetSDK.NET_DVR_PTZControl_Other(m_lUserID, iChannelNum[(int)iSelIndex], 22, 1);
        }

        private void button6_MouseDown(object sender, MouseEventArgs e)
        {
            bool test = CHCNetSDK.NET_DVR_PTZControl_Other(m_lUserID, iChannelNum[(int)iSelIndex], 23, 0);
        }

        private void button6_MouseUp(object sender, MouseEventArgs e)
        {
            bool test = CHCNetSDK.NET_DVR_PTZControl_Other(m_lUserID, iChannelNum[(int)iSelIndex], 23, 1);
        }

        private void button7_MouseDown(object sender, MouseEventArgs e)
        {
            bool test = CHCNetSDK.NET_DVR_PTZControl_Other(m_lUserID, iChannelNum[(int)iSelIndex], 24, 0);
        }

        private void button7_MouseUp(object sender, MouseEventArgs e)
        {
            bool test = CHCNetSDK.NET_DVR_PTZControl_Other(m_lUserID, iChannelNum[(int)iSelIndex], 24, 1);
        }
    }

    }
