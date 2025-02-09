﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Configuration;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Xml;
using System.Collections;
using System.Runtime.InteropServices;
namespace InterfaceHardware
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        

        private void frmMain_Load(object sender, EventArgs e)
        {
            Thread.Sleep(3000);
            this.Visible = false;
            try
            {
                xDoc = new XmlDocument();
                xDoc.Load(Application.StartupPath + "\\Brand\\" + ConfigurationManager.AppSettings["Brand"].Trim() + ".xml");
                //XmlElement root = xDoc.DocumentElement;
                //XmlNode node = root.SelectSingleNode("personalSettings");
                //nodelist = node.SelectNodes("add");
                if(xDoc.InnerText!="")
                {
                    MessageBox.Show("Brandxml加载失败");
                    Writelog("Brandxml加载失败", "Load", "ErrLog.txt");
                    //TextBoxLog("Brandxml加载失败", "Load", true);
                }
                bOpen();//打开控制板
                TCPOpen();//启动TCP
                timLED.Enabled = true;
            }
            catch (Exception ex)
            {
                Writelog(ex.ToString(), "Load", "ErrLog.txt");
            }
            
        }
        private XmlDocument xDoc = null;
        private Socket sockHeartbeat;
        private bool issuspend = false;
        //private bool isopenbox = false;
        Thread thListener = null;
        private void TCPOpen()
        {
            //listener = new TcpListener(new ipendp);
            try
            {
                string port = ConfigurationManager.AppSettings["Port"].Trim();
                int HeartbeatPort = int.Parse(port);//这里是端口 一般从配置文件读出来比较好
                string ip = ConfigurationManager.AppSettings["IP"].Trim();
                IPEndPoint EPHeartbeat = new IPEndPoint(IPAddress.Parse(ip), HeartbeatPort);
                sockHeartbeat = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //实例化套接字指定双向流tcp协议。
                sockHeartbeat.Bind(EPHeartbeat);//相当于把ip和端口绑定到套接字对象
                sockHeartbeat.Listen(50);//开启监听，30为最大数量这个随意
                thListener = new Thread(new ThreadStart(SmartListener)) { IsBackground = true }; //后台线程循环监听
                thListener.Start();//开启
                //SaveLog("Socket", "Socketopen", "Socket服务启动", DateTime.Now);
                Writelog("Socket服务启动", "TCPOpen", "SystemLog.txt");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                //ErrLog("辅助程序", "socketopen", ex.ToString(), DateTime.Now);
                Writelog(ex.ToString(), "TCPOpen", "ErrLog.txt");
            }
        }
        //定义一个集合，存储客户端信息
        static Dictionary<string, Socket> clientConnectionItems = new Dictionary<string, Socket> { };
        private List<Socket> Lst_SokServer = new List<Socket>(); //套接字集合。
        /// <summary>
        /// 循环等待连接并开启接收线程
        /// </summary>
        private void SmartListener()
        {
            Socket acceptSock = null;
            while (true) //用死循环是为了多客户端考虑
            {
                try
                {
                    acceptSock = sockHeartbeat.Accept();
                    // 只要没有客户端连接，这里就会一直堵塞，程序不会往下走 ，所以要用到上面的多线程; 当客户端连接上时，我们重新定义一个套接字叫accetpSock将取代原先定义的全局变量的套接字sockHeartbeat，所以之后用到就一直是acceptSock。
                }
                catch (Exception ex)
                {
                }

                //if (!Lst_SokServer.Contains(acceptSock))
                //{
                //    Lst_SokServer.Add(acceptSock);
                //    //假如集合内不包含此套接字，便加入集合，这个是为了多客户端考虑。
                //    Thread threadMsg = new Thread(WatchMsg) { Name = "ThdMsgRec", IsBackground = true };
                //    threadMsg.Start();
                //    //开启一个接收客户端程序消息的线程，因为这里也要做循环接收
                //}

                try
                {
                    //获取客户端的IP和端口号  
                    IPAddress clientIP = (acceptSock.RemoteEndPoint as IPEndPoint).Address;
                    int clientPort = (acceptSock.RemoteEndPoint as IPEndPoint).Port;
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        this.textBox1.Text += "客户端IP:" + clientIP.ToString() ;
                    });
                    //客户端网络结点号  
                    string remoteEndPoint = acceptSock.RemoteEndPoint.ToString();
                    //添加客户端信息  
                    clientConnectionItems.Add(remoteEndPoint, acceptSock);
                    IPEndPoint netpoint = acceptSock.RemoteEndPoint as IPEndPoint;
                    //创建一个通信线程
                    ParameterizedThreadStart pts = new ParameterizedThreadStart(delegate { RecvMsg(acceptSock, this); });
                    Thread thread = new Thread(pts);
                    thread.IsBackground = true;
                    thread.Start();
                }
                catch (Exception ex)
                {
                    //Writelog(ee.ToString(), "SmartListener", "ErrLog.txt");
                    //ErrLog("辅助程序", "SmartListener", ee.ToString(), DateTime.Now);
                    Writelog(ex.ToString(), "SmartListener", "ErrLog.txt");
                    if (acceptSock.Connected)
                        acceptSock.Close();
                }
            }
        }
        public class SetMessage
        {
            public void setmessage(string mes, frmMain frm)
            {
                frm.BeginInvoke((MethodInvoker)delegate
                {
                    //frm.TextBoxLog(mes, "RecvMsg", true);
                });
            }
            public void setlog(string mes, frmMain frm)
            {
                frm.BeginInvoke((MethodInvoker)delegate
                {
                    frm.Writelog(mes, "RecvMsg", "heartlog.txt");
                });
            }
            public void seterrlog(string mes, frmMain frm)
            {
                frm.BeginInvoke((MethodInvoker)delegate
                {
                    frm.Writelog(mes, "RecvMsg", "ErrLog.txt");
                });
            }
            public void StartReadComMachine(frmMain frm)
            {
                frm.BeginInvoke((MethodInvoker)delegate
                {
                    frm.ReadComMachine = new Thread(new ThreadStart(delegate { frm.ReadCommPort1(frm); }));
                    CheckForIllegalCrossThreadCalls = false;
                    frm.ReadComMachine.IsBackground = true;
                    frm.ReadComMachine.Start();
                });
            }
            public void settimLED(frmMain frm,bool state)
            {
                frm.BeginInvoke((MethodInvoker)delegate
                {
                    frm.timLED.Enabled = state;
                });
            }
        }
        static Dictionary<BrandCommand.RevBoxData, Socket> ListCode = new Dictionary<BrandCommand.RevBoxData, Socket>(new BrandCommand.EqualityComparer()) { };
        static void RecvMsg(object socketclientpara, frmMain frm)
        {
            Socket socketServer = socketclientpara as Socket;
            try
            {
                while (true)
                {
                    if (socketServer != null && socketServer.Connected == true)
                    {
                        //开始等待接受消息,开辟缓存
                        byte[] arrMsg = new byte[1024 * 64];
                        //将真正接到的数据的长度返回
                        int inLen = socketServer.Receive(arrMsg);
                        //假如客户端并没有发消息 则忽略跳过继续接收
                        if (inLen == 0) { continue; }
                        //将实际客户端发来的字节数复制到一个新的数组，否则缓存过大后面将产产生过多的00000；缓存过小则对某些数据比如图片流 不能一次性接收及时显示，还需要根据不同的图片格式进行图片头和尾的切割。
                        byte[] NewByte = new byte[inLen];
                        Array.Copy(arrMsg, 0, NewByte, 0, inLen);
                        //进入拆包方法，这里请先看方法 调用完成之后继续往下看
                        //string s = Encoding.Unicode.GetString(NewByte);
                        SetMessage a = new SetMessage();
                        a.setlog("Receive:" + frm.ByteToString(NewByte), frm);
                        a.setmessage("Receive:" + frm.ByteToString(NewByte), frm);
                        if (NewByte[0]==0xFF&& NewByte[NewByte.Length - 1] == 0xFE)
                        {
                            BrandCommand.RevBoxData revdata = new BrandCommand.RevBoxData();
                            XmlNode node = frm.xDoc.SelectSingleNode("//configuration/personalSettings/add[@key=\"" + NewByte[2] + "\"]");
                            byte box = (byte)Convert.ToInt32(node.Attributes["value"].Value);
                            revdata.Box = box;
                            revdata.CardAddr= NewByte[1];
                            //if (ListCode.ContainsKey(revdata))//已开门
                            //{
                            //    byte[] w = new byte[6];
                            //    w[0] = 0xFE;
                            //    w[1] = NewByte[1];
                            //    w[2] = NewByte[2];
                            //    w[3] = 0x00;
                            //    w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                            //    w[5] = 0xFF;
                            //    socketServer.Send(w);
                            //}
                            //else
                            //{
                            if(frm.ReadComMachine.ThreadState != ThreadState.Background)
                            {
                                a.StartReadComMachine(frm);
                            }
                                frm.issuspend = true;
                                //Thread.Sleep(600);
                            if (NewByte[3] == 0x01)
                            {
                                //
                                if (ListCode.ContainsKey(revdata))
                                {
                                    byte[] w = new byte[6];
                                    w[0] = 0xFE;
                                    w[1] = revdata.CardAddr;
                                    w[2] = NewByte[2];
                                    w[3] = 0x00;
                                    w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                                    w[5] = 0xFF;
                                    socketServer.Send(w);
                                    frm.issuspend = false;
                                }
                                else
                                {
                                    ListCode.Add(revdata, socketServer);
                                    byte[] writearr = BrandCommand.OpenBox(box, NewByte[1]);
                                    frm.serialPort1.Write(writearr, 0, writearr.Length);
                                    frm.Writelog("发送串口命令:" + frm.ByteToString(writearr), "RecvMsg", "SystemLog.txt");
                                    Thread.Sleep(500);
                                    byte[] w = new byte[6];
                                    w[0] = 0xFE;
                                    w[1] = revdata.CardAddr;
                                    w[2] = NewByte[2];
                                    w[3] = 0x01;
                                    w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                                    w[5] = 0xFF;
                                    socketServer.Send(w);
                                    frm.Writelog("TCP回复命令:" + frm.ByteToString(w) + "," + revdata.CardAddr.ToString()+","+ revdata.Box.ToString(), "ReadCommPort1", "SystemLog.txt");
                                    listledtime.Add(revdata, DateTime.Now);
                                    frm.issuspend = false;
                                }
                            }
                            else if (NewByte[3] == 0x02)//维修开锁
                            {
                                try
                                {
                                    frm.issuspend = true;
                                    Thread.Sleep(600);
                                    byte[] writearr = BrandCommand.OpenALL(NewByte[1]);
                                    frm.serialPort1.Write(writearr, 0, writearr.Length);
                                    frm.Writelog("发送串口命令:" + frm.ByteToString(writearr), "RecvMsg", "SystemLog.txt");
                                    //a.settimLED(frm, false);
                                    byte[] w = new byte[6];
                                    w[0] = 0xFE;
                                    w[1] = NewByte[1];
                                    w[2] = 0x00;
                                    w[3] = 0x02;
                                    w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                                    w[5] = 0xFF;
                                    socketServer.Send(w);
                                    for (int i = 1; i <= 60; i++)
                                    {
                                        BrandCommand.RevBoxData tempdata = new BrandCommand.RevBoxData();
                                        tempdata.Box = (byte)i;
                                        tempdata.CardAddr = revdata.CardAddr;
                                        if (listledtime.ContainsKey(tempdata))
                                        {
                                            listledtime.Remove(tempdata);
                                        }
                                        writearr = BrandCommand.LEDControl(tempdata.Box, 0x30, tempdata.CardAddr);//关闭红灯
                                        frm.serialPort1.Write(writearr, 0, writearr.Length);
                                        frm.Writelog("发送串口命令:" + frm.ByteToString(writearr), "RecvMsg", "SystemLog.txt");
                                        Thread.Sleep(500);
                                    }
                                    frm.issuspend = false;
                                    //a.settimLED(frm, true);
                                }
                                catch
                                {
                                    byte[] w = new byte[6];
                                    w[0] = 0xFE;
                                    w[1] = NewByte[1];
                                    w[2] = 0x00;
                                    w[3] = 0xFF;
                                    w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                                    w[5] = 0xFF;
                                    socketServer.Send(w);
                                }
                                //frm.TextBoxLog("发送串口命令:" + frm.ByteToString(writearr), "RecvMsg", false);
                            }
                            else if (NewByte[3] == 0x03)//重置外挂
                            {
                                byte[] w = new byte[6];
                                w[0] = 0xFE;
                                w[1] = 0x00;
                                w[2] = 0x00;
                                w[3] = 0x03;
                                w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                                w[5] = 0xFF;
                                socketServer.Send(w);
                                frm.Writelog("TCP回复命令:" + frm.ByteToString(w), "ReadCommPort1", "SystemLog.txt");
                                Thread.Sleep(200);
                                foreach (BrandCommand.RevBoxData item in ListCode.Keys.ToArray())
                                {
                                    ListCode[item].Send(w);
                                    frm.Writelog("TCP回复命令:" + frm.ByteToString(w) , "ReadCommPort1", "SystemLog.txt");
                                    ListCode.Remove(item);
                                    Thread.Sleep(500);
                                }
                                listledtime.Clear();
                                frm.ReadComMachine.Abort();
                                Thread.Sleep(500);
                                a.StartReadComMachine(frm);
                            }
                            //}
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {

                clientConnectionItems.Remove(socketServer.RemoteEndPoint.ToString());
                //关闭之前accept出来的和客户端进行通信的套接字 
                //socketServer.Send(Encoding.Unicode.GetBytes("$|ERR|￥"));
                //FileStream fs1 = new FileStream("ErrLog.txt", FileMode.Append);
                //StreamWriter sw1 = new StreamWriter(fs1);
                //sw1.WriteLine("socket,"+ex.ToString() + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                //sw1.Close();
                SetMessage a = new SetMessage();
                a.setlog(ex.ToString(), frm);
                if (socketServer.Connected)
                    socketServer.Close();
            }
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="log">日志说明</param>
        /// <param name="tag">标注位置</param>
        /// <param name="flie">所写文件</param>
        private void Writelog(string log, string tag, string flie)
        {
            try
            {
                string path = @"log/" + DateTime.Now.ToString("yyyy-MM-dd");
                if (Directory.Exists(path) == false)//如果不存在就创建文件夹 
                {
                    Directory.CreateDirectory(path);

                }
                FileStream fs = new FileStream(path + "/" + flie, FileMode.Append);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + log + "," + tag);
                sw.Close();
            }
            catch
            {

            }
        }
        //private void TextBoxLog(string log, string tag, bool clear)
        //{
        //    if (clear)
        //    {
        //        this.textBox1.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + log + "," + tag + "\r\n";
        //    }
        //    else
        //    {
        //        this.textBox1.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + log + "," + tag + "\r\n" + this.textBox1.Text;
        //    }
        //}
        private string ByteToString(byte[] arr)
        {
            StringBuilder strB1 = new StringBuilder();
            for (int k = 0; k < arr.Length; k++)
            {
                strB1.Append(arr[k].ToString("X2") + " ");
            }
            return strB1.ToString();
        }
        private void bOpen()
        {
            this.serialPort1.PortName = ConfigurationManager.AppSettings["ControlCom"].Trim();
            this.serialPort1.BaudRate = Convert.ToInt32(ConfigurationManager.AppSettings["ControlBaudRate"].Trim());//波特率
            try
            {
                if (ConfigurationManager.AppSettings["LinkMode"].Trim() == "Com")
                {
                    this.serialPort1.Open();
                    byte[] test = BrandCommand.GetState(0x01);//查询一下是否有反馈,确认硬件连通
                    this.serialPort1.Write(test, 0, test.Length);
                    //byte[] writearr = BrandCommand.LEDControl(0x01,0x31);//打开红灯
                    //serialPort1.Write(writearr, 0, writearr.Length);
                }
                else if (ConfigurationManager.AppSettings["LinkMode"].Trim() == "TCP")
                {

                }
            }
            catch(Exception ex)
            {
                //TextBoxLog("控制板串口打开失败", "bOpen", false);
                MessageBox.Show("控制板串口打开失败,请检查系统后重新启动辅助程序");
                Writelog(ex.ToString(), "bOpen", "ErrLog.txt");
                return;
            }
            try
            {
                ReadComMachine = new Thread(new ThreadStart(delegate { ReadCommPort1(this); }));
                CheckForIllegalCrossThreadCalls = false;
                ReadComMachine.IsBackground = true;
                ReadComMachine.Start();
            }
            catch (Exception ex)
            {
                //TextBoxLog("控制板端口侦听错误", "bOpen", false);
                MessageBox.Show("服务启动失败,请检查网络IP后重新启动辅助程序");
                Writelog(ex.ToString(), "bOpen", "ErrLog.txt");
            }
        }
        Thread ReadComMachine;
        private void ReadCommPort1(frmMain frm)
        {
            try
            {
                SetMessage a = new SetMessage();
                while (this.serialPort1.IsOpen)
                {
                    int read = serialPort1.BytesToRead;
                    int len = 20;
                    if (ConfigurationManager.AppSettings["Brand"].Trim() == "HC")
                    {
                        len = 20;
                    }
                    else
                    {
                        len = 6;
                    }
                    byte[] result = new byte[len];
                    if (read < len)
                    {
                        Thread.Sleep(300);
                        continue;
                    }
                    else
                    {
                        serialPort1.Read(result, 0, result.Length);
                        Writelog("串口接收命令:" + ByteToString(result), "ReadCommPort1", "SystemLog.txt");
                        if (ConfigurationManager.AppSettings["Brand"].Trim() == "HC")
                        {
                            if (result[0] != 0x02 || result[19] != 0x03)
                            {
                                serialPort1.DiscardInBuffer();
                                continue;
                            }
                        }
                        BrandCommand.ResultData resultdata = new BrandCommand.ResultData();
                        if (ConfigurationManager.AppSettings["Brand"].Trim() == "HC")
                            resultdata = BrandCommand.HCResultAnalyse(result);
                        else
                            resultdata = BrandCommand.SNResultAnalyse(result);
                        if (ConfigurationManager.AppSettings["Brand"].Trim() == "HC")
                        {
                            BrandCommand.RevBoxData key = new BrandCommand.RevBoxData();
                            key.Box = resultdata.Box;
                            key.CardAddr = resultdata.CardAddr;
                            if (resultdata.Command == 0x4C)//开锁状态
                            {
                                if (resultdata.State == 0x30)//关闭状态
                                {
                                    byte[] w = new byte[6];
                                    w[0] = 0xFE;
                                    w[1] = resultdata.CardAddr;
                                    w[2] = resultdata.Box;
                                    w[3] = 0x02;
                                    w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                                    w[5] = 0xFF;
                                    if (ListCode.ContainsKey(key))
                                    {
                                        ListCode[key].Send(w);
                                        ListCode.Remove(key);
                                        Writelog("TCP回复命令:" + ByteToString(w) + "," + key.CardAddr.ToString()+","+key.Box.ToString(), "ReadCommPort1", "SystemLog.txt");
                                    }
                                    if (listledtime.ContainsKey(key))
                                    {
                                        listledtime.Remove(key);
                                    }
                                }
                                else if(resultdata.State == 0x32)//超时
                                {
                                    byte[] w = new byte[6];
                                    w[0] = 0xFE;
                                    w[1] = resultdata.CardAddr;
                                    w[2] = resultdata.Box;
                                    w[3] = 0x03;
                                    w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                                    w[5] = 0xFF;
                                    if (ListCode.ContainsKey(key))
                                    {
                                        ListCode[key].Send(w);
                                        ListCode.Remove(key);
                                        Writelog("TCP回复命令:" + ByteToString(w) + "," + key.CardAddr.ToString() + "," + key.Box.ToString(), "ReadCommPort1", "SystemLog.txt");
                                    }
                                    if (listledtime.ContainsKey(key))
                                    {
                                        listledtime.Remove(key);
                                    }
                                    frm.issuspend = true;
                                    Thread.Sleep(600);
                                    //a.settimLED(frm, false);
                                    byte[] writearr = BrandCommand.LEDControl(resultdata.Box, 0x30, resultdata.CardAddr);//关闭所有灯
                                    serialPort1.Write(writearr, 0, writearr.Length);
                                    Writelog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", "SystemLog.txt");
                                    Thread.Sleep(500);
                                    writearr = BrandCommand.LEDControl(resultdata.Box, 0x31, resultdata.CardAddr);//打开红灯
                                    serialPort1.Write(writearr, 0, writearr.Length);
                                    Writelog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", "SystemLog.txt");
                                    Thread.Sleep(500);
                                    frm.issuspend = false;
                                    //a.settimLED(frm, true);
                                }
                                else//37开门异常
                                {
                                    byte[] w = new byte[6];
                                    w[0] = 0xFE;
                                    w[1] = resultdata.CardAddr;
                                    w[2] = resultdata.Box;
                                    w[3] = 0xFF;
                                    w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                                    w[5] = 0xFF;
                                    if (ListCode.ContainsKey(key))
                                    {
                                        ListCode[key].Send(w);
                                        ListCode.Remove(key);
                                        Writelog("TCP回复命令:" + ByteToString(w) + "," + key.CardAddr.ToString() + "," + key.Box.ToString(), "ReadCommPort1", "SystemLog.txt");
                                    }
                                    if (listledtime.ContainsKey(key))
                                    {
                                        listledtime.Remove(key);
                                    }
                                }
                            }
                            #region **********
                            //if (isopenbox)
                            //{
                            //    if (resultdata.Command == 0x30)//查询状态
                            //    {
                            //        if (resultdata.State == 0x30)//关闭状态
                            //        {
                            //            byte[] writearr = BrandCommand.OpenBox(resultdata.Box, resultdata.CardAddr);//执行开门
                            //            serialPort1.Write(writearr, 0, writearr.Length);
                            //            Writelog("发送串口命令:" + ByteToString(writearr)+","+key.ToString(), "ReadCommPort1", "SystemLog.txt");
                            //            Thread.Sleep(500);
                            //            continue;
                            //        }
                            //        else if (resultdata.State == 0x31)//打开状态
                            //        {
                            //            byte[] w = new byte[6];
                            //            w[0] = 0xFE;
                            //            w[1] = resultdata.CardAddr;
                            //            w[2] = resultdata.Box;
                            //            w[3] = 0x00;
                            //            w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                            //            w[5] = 0xFF;
                            //            if (ListCode.ContainsKey(key))
                            //            {
                            //                ListCode[key].Send(w);
                            //                //ListCode.Remove(key);
                            //                Writelog("TCP回复命令:" + ByteToString(w) + "," + key.ToString(), "ReadCommPort1", "SystemLog.txt");
                            //            }
                            //            a.settimLED(frm, false);
                            //            if (!listledtime.ContainsKey(key))
                            //            {
                            //                if (ConfigurationManager.AppSettings["LedTime"].Trim() != "")
                            //                {
                                                
                            //                    int ledtime = Convert.ToInt32(ConfigurationManager.AppSettings["LedTime"].Trim());
                            //                    //timLED.Enabled = true;
                            //                    listledtime.Add(key, ledtime);
                                               
                            //                }
                            //            }
                            //            //byte[] writearr = BrandCommand.GetState(resultdata.Box, resultdata.CardAddr);//继续查询状态
                            //            //serialPort1.Write(writearr, 0, writearr.Length);
                            //            //Writelog("发送串口命令:" + ByteToString(writearr) + "," + key.ToString(), "ReadCommPort1", "SystemLog.txt");
                            //            //Thread.Sleep(500);
                            //            issuspend = false;
                            //            isopenbox = false;
                            //            Thread.Sleep(500);
                            //            a.settimLED(frm, true);
                            //            continue;
                            //        }
                            //        else
                            //        {
                            //            byte[] w = new byte[6];
                            //            w[0] = 0xFE;
                            //            w[1] = resultdata.CardAddr;
                            //            w[2] = resultdata.Box;
                            //            w[3] = 0xFF;
                            //            w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                            //            w[5] = 0xFF;
                            //            if (ListCode.ContainsKey(key))
                            //            {
                            //                ListCode[key].Send(w);
                            //                ListCode.Remove(key);
                            //                Writelog("TCP回复命令:" + ByteToString(w) + "," + key.ToString(), "ReadCommPort1", "SystemLog.txt");
                            //            }
                                        
                            //            //TextBoxLog("TCP回复命令:" + ByteToString(w), "ReadCommPort1", false);
                            //        }
                            //    }
                            //    else if (resultdata.Command == 0x47)//开门
                            //    {
                            //        if (resultdata.State == 0x31)//执行成功
                            //        {
                            //            byte[] w = new byte[6];
                            //            w[0] = 0xFE;
                            //            w[1] = resultdata.CardAddr;
                            //            w[2] = resultdata.Box;
                            //            w[3] = 0x01;
                            //            w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                            //            w[5] = 0xFF;
                            //            if (ListCode.ContainsKey(key))
                            //            {
                            //                ListCode[key].Send(w);
                            //                //ListCode.Remove(key);
                            //                Writelog("TCP回复命令:" + ByteToString(w) + "," + key.ToString(), "ReadCommPort1", "SystemLog.txt");
                            //            }
                            //            if (ConfigurationManager.AppSettings["LedTime"].Trim() != "")
                            //            {
                            //                a.settimLED(frm, false);
                            //                byte[] writearrled = BrandCommand.LEDControl(resultdata.Box, 0x32, resultdata.CardAddr);//打开绿灯
                            //                serialPort1.Write(writearrled, 0, writearrled.Length);
                            //                Writelog("发送串口命令:" + ByteToString(writearrled) + "," + key.ToString(), "ReadCommPort1", "SystemLog.txt");
                            //                Thread.Sleep(500);
                            //                int ledtime = Convert.ToInt32(ConfigurationManager.AppSettings["LedTime"].Trim());
                            //                //timLED.Enabled = true;
                            //                listledtime.Add(key, ledtime);
                            //                a.settimLED(frm, true);
                            //            }
                            //            //byte[] writearr = BrandCommand.GetState(resultdata.Box, resultdata.CardAddr);//继续查询状态
                            //            //serialPort1.Write(writearr, 0, writearr.Length);
                            //            //Writelog("发送串口命令:" + ByteToString(writearr) + "," + key.ToString(), "ReadCommPort1", "SystemLog.txt");
 
                            //            Thread.Sleep(500);
                            //            issuspend = false;
                            //            isopenbox = false;

                            //        }
                            //        else//执行失败
                            //        {
                            //            byte[] w = new byte[6];
                            //            w[0] = 0xFE;
                            //            w[1] = resultdata.CardAddr;
                            //            w[2] = resultdata.Box;
                            //            w[3] = 0xFF;
                            //            w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                            //            w[5] = 0xFF;
                            //            if (ListCode.ContainsKey(key))
                            //            {
                            //                ListCode[key].Send(w);
                            //                ListCode.Remove(key);
                            //                Writelog("TCP回复命令:" + ByteToString(w) + "," + key.ToString(), "ReadCommPort1", "SystemLog.txt");
                            //            }
                            //            issuspend = false;
                            //            //TextBoxLog("TCP回复命令:" + ByteToString(w), "ReadCommPort1", false);
                            //            isopenbox = false;
                            //        }
                            //    }
                            //}
                            //else
                            //{
                            //    if (resultdata.Command == 0x30)//查询状态
                            //    {
                            //        if (listledtime.ContainsKey(key))
                            //        {
                            //            if (resultdata.State == 0x31 && listledtime[key] < 0)//打开状态且超时
                            //            {
                            //                if (listledtime[key] > -10)
                            //                {
                            //                    frm.issuspend = true;
                            //                    Thread.Sleep(600);
                            //                    a.settimLED(frm, false);
                            //                    byte[] writearr = BrandCommand.LEDControl(resultdata.Box, 0x30, resultdata.CardAddr);//关闭所有灯
                            //                    serialPort1.Write(writearr, 0, writearr.Length);
                            //                    Writelog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", "SystemLog.txt");
                            //                    Thread.Sleep(500);
                            //                    writearr = BrandCommand.LEDControl(resultdata.Box, 0x31, resultdata.CardAddr);//打开红灯
                            //                    serialPort1.Write(writearr, 0, writearr.Length);
                            //                    Writelog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", "SystemLog.txt");
                            //                    Thread.Sleep(500);
                            //                    frm.issuspend = false;
                            //                    a.settimLED(frm, true);
                            //                }
                            //                //writearr = BrandCommand.GetState(resultdata.Box, resultdata.CardAddr);//继续查询
                            //                //serialPort1.Write(writearr, 0, writearr.Length);
                            //                //Writelog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", "SystemLog.txt");
                            //                //Thread.Sleep(500);
                            //            }
                            //            else if (resultdata.State == 0x31 && listledtime[key] >= 0)//打开状态未超时
                            //            {
                            //                //byte[] writearr = BrandCommand.GetState(resultdata.Box, resultdata.CardAddr);//继续查询
                            //                //serialPort1.Write(writearr, 0, writearr.Length);
                            //                //Writelog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", "SystemLog.txt");
                            //                //Thread.Sleep(500);
                            //                if (!timLED.Enabled)
                            //                {
                            //                    a.settimLED(frm, true);
                            //                }
                            //            }
                            //            else if (resultdata.State == 0x30)//关闭状态
                            //            {
                            //                //ledtime = 0;
                            //                frm.issuspend = true;
                            //                Thread.Sleep(600);
                            //                a.settimLED(frm, false);
                            //                listledtime.Remove(key);
                            //                byte[] writearr = BrandCommand.LEDControl(resultdata.Box, 0x30, resultdata.CardAddr);//关闭红灯
                            //                serialPort1.Write(writearr, 0, writearr.Length);
                            //                Writelog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", "SystemLog.txt");
                            //                //TextBoxLog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", false);
                            //                Thread.Sleep(500);
                            //                byte[] w = new byte[6];
                            //                w[0] = 0xFE;
                            //                w[1] = resultdata.CardAddr;
                            //                w[2] = resultdata.Box;
                            //                w[3] = 0x02;
                            //                w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                            //                w[5] = 0xFF;
                            //                if (ListCode.ContainsKey(key))
                            //                {
                            //                    ListCode[key].Send(w);
                            //                    ListCode.Remove(key);
                            //                    Writelog("TCP回复命令:" + ByteToString(w) + "," + key.ToString(), "ReadCommPort1", "SystemLog.txt");
                            //                }
                            //                frm.issuspend = false;
                            //                a.settimLED(frm, true);
                            //            }
                            //            //foreach (BrandCommand.RevBoxData item in listledtime.Keys.ToArray())
                            //            //{
                            //            //    if (!issuspend)
                            //            //    {
                            //            //        byte[] writearr = BrandCommand.GetState(item.Box, item.CardAddr);//继续查询
                            //            //        serialPort1.Write(writearr, 0, writearr.Length);
                            //            //        Writelog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", "SystemLog.txt");
                            //            //        Thread.Sleep(300);
                            //            //    }
                            //            //}
                            //        }
                            //    }
                                #region #####
                            //    //if (resultdata.Command == 0x34)//维修开锁
                            //    //{
                            //    //    if (resultdata.State == 0x5A )//执行成功
                            //    //    {
                            //    //        frm.issuspend = true;
                            //    //        Thread.Sleep(600);
                            //    //        a.settimLED(frm, false);
                            //    //        byte[] w = new byte[6];
                            //    //        w[0] = 0xFE;
                            //    //        w[1] = resultdata.CardAddr;
                            //    //        w[2] = 0x00;
                            //    //        w[3] = 0x02;
                            //    //        w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                            //    //        w[5] = 0xFF;
                            //    //        ListCode[key].Send(w);
                            //    //        ListCode.Remove(key);
                            //    //        for (int i = 1; i <= 60; i++)
                            //    //        {
                            //    //            BrandCommand.RevBoxData revdata = new BrandCommand.RevBoxData();
                            //    //            revdata.Box = (byte)i;
                            //    //            revdata.CardAddr = resultdata.CardAddr;
                            //    //            if(listledtime.ContainsKey(revdata))
                            //    //            {
                            //    //                listledtime.Remove(revdata);
                            //    //            }
                            //    //            byte[] writearr = BrandCommand.LEDControl(revdata.Box, 0x30, revdata.CardAddr);//关闭红灯
                            //    //            serialPort1.Write(writearr, 0, writearr.Length);
                            //    //            Writelog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", "SystemLog.txt");
                            //    //            Thread.Sleep(500);
                            //    //        }
                            //    //        frm.issuspend = false;
                            //    //        a.settimLED(frm, true);
                            //    //    }
                            //    //    else
                            //    //    {
                            //    //        byte[] w = new byte[6];
                            //    //        w[0] = 0xFE;
                            //    //        w[1] = resultdata.CardAddr;
                            //    //        w[2] = 0x00;
                            //    //        w[3] = 0xFF;
                            //    //        w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                            //    //        w[5] = 0xFF;
                            //    //        ListCode[key].Send(w);
                            //    //        ListCode.Remove(key);
                            //    //    }
                            //    //}
                                #endregion
                            //}
                            #endregion
                        }
                        #region SN板
                        //else//SN板
                        //{
                        //    if (ListCode.ContainsKey(resultdata.Box))
                        //    {
                        //        if (resultdata.Command == 0x80)//查询状态
                        //        {
                        //            if (resultdata.State == 0x00)//关闭状态
                        //            {
                        //                byte[] writearr = BrandCommand.OpenBox(resultdata.Box);//执行开门
                        //                serialPort1.Write(writearr, 0, writearr.Length);
                        //                Writelog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", "SystemLog.txt");
                        //                //TextBoxLog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", false);
                        //                return;
                        //            }
                        //            else if (resultdata.State == 0x11)//打开状态
                        //            {
                        //                byte[] w = new byte[5];
                        //                w[0] = 0xFE;
                        //                w[1] = resultdata.Box;
                        //                w[2] = 0x00;
                        //                w[3] = (byte)(w[1] ^ w[2]);
                        //                w[4] = 0xFF;
                        //                ListCode[resultdata.Box].Send(w);
                        //                ListCode.Remove(resultdata.Box);
                        //                Writelog("TCP回复命令:" + ByteToString(w), "ReadCommPort1", "SystemLog.txt");
                        //                //TextBoxLog("TCP回复命令:" + ByteToString(w), "ReadCommPort1", false);
                        //            }
                        //            else
                        //            {
                        //                byte[] w = new byte[5];
                        //                w[0] = 0xFE;
                        //                w[1] = resultdata.Box;
                        //                w[2] = 0xFF;
                        //                w[3] = (byte)(w[1] ^ w[2]);
                        //                w[4] = 0xFF;
                        //                ListCode[resultdata.Box].Send(w);
                        //                ListCode.Remove(resultdata.Box);
                        //                Writelog("TCP回复命令:" + ByteToString(w), "ReadCommPort1", "SystemLog.txt");
                        //                //TextBoxLog("TCP回复命令:" + ByteToString(w), "ReadCommPort1", false);
                        //            }
                        //        }
                        //        else if (resultdata.Command == 0x8A)//开门
                        //        {
                        //            if (resultdata.State == 0x11)//执行成功
                        //            {
                        //                byte[] w = new byte[5];
                        //                w[0] = 0xFE;
                        //                w[1] = resultdata.Box;
                        //                w[2] = 0x01;
                        //                w[3] = (byte)(w[1] ^ w[2]);
                        //                w[4] = 0xFF;
                        //                ListCode[resultdata.Box].Send(w);
                        //                ListCode.Remove(resultdata.Box);
                        //                Writelog("TCP回复命令:" + ByteToString(w), "ReadCommPort1", "SystemLog.txt");
                        //                //TextBoxLog("TCP回复命令:" + ByteToString(w), "ReadCommPort1", false);
                        //                if (ConfigurationManager.AppSettings["LedTime"].Trim() != "")
                        //                {
                        //                    ledtime = Convert.ToInt32(ConfigurationManager.AppSettings["LedTime"].Trim());
                        //                    //timLED.Enabled = true;
                        //                    a.settimLED(frm, true);
                        //                }

                        //            }
                        //            else//执行失败
                        //            {
                        //                byte[] w = new byte[5];
                        //                w[0] = 0xFE;
                        //                w[1] = resultdata.Box;
                        //                w[2] = 0xFF;
                        //                w[3] = (byte)(w[1] ^ w[2]);
                        //                w[4] = 0xFF;
                        //                ListCode[resultdata.Box].Send(w);
                        //                ListCode.Remove(resultdata.Box);
                        //                Writelog("TCP回复命令:" + ByteToString(w), "ReadCommPort1", "SystemLog.txt");
                        //                //TextBoxLog("TCP回复命令:" + ByteToString(w), "ReadCommPort1", false);
                        //            }
                        //        }
                        //    }
                        //    else
                        //    {
                        //        if (resultdata.Command == 0x80)//查询状态
                        //        {
                        //            if (resultdata.State == 0x11 && ledtime < 0)//打开状态且超时
                        //            {
                        //                byte[] writearr = BrandCommand.LEDControl(resultdata.Box, 0x31, resultdata.CardAddr);//打开红灯
                        //                serialPort1.Write(writearr, 0, writearr.Length);
                        //                Writelog("发送串口命令:" + ByteToString(writearr), "Load", "SystemLog.txt");
                        //                //TextBoxLog("发送串口命令:" + ByteToString(writearr), "Load", false);
                        //                Thread.Sleep(500);
                        //            }
                        //            else if (resultdata.State == 0x11 && ledtime >= 0)//打开状态未超时
                        //            {
                        //                byte[] writearr = BrandCommand.GetState(resultdata.Box, resultdata.CardAddr);
                        //                serialPort1.Write(writearr, 0, writearr.Length);
                        //                Writelog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", "SystemLog.txt");
                        //                //TextBoxLog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", false);
                        //                Thread.Sleep(500);
                        //            }
                        //            else if (resultdata.State == 0x00)//关闭状态
                        //            {
                        //                ledtime = 0;
                        //                //timLED.Enabled = false;
                        //                a.settimLED(frm, false);
                        //            }
                        //        }
                        //    }
                        //}
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                //TextBoxLog("控制板通讯错误", "ReadCommPort1", false);
                Writelog(ex.Message, "ReadCommPort1", "ErrLog.txt");
                //ReadComMachine.Abort();
                //this.serialPort1.Close();
            }
            //finally
            //{
            //    if (ReadComMachine.ThreadState != ThreadState.Background)
            //    {
            //        ReadComMachine = new Thread(new ThreadStart(delegate { ReadCommPort1(this); }));
            //        CheckForIllegalCrossThreadCalls = false;
            //        ReadComMachine.IsBackground = true;
            //        ReadComMachine.Start();
            //    }
            //}
        }
        //int ledtime = 0;
        static Dictionary<BrandCommand.RevBoxData, DateTime> listledtime=new Dictionary<BrandCommand.RevBoxData, DateTime>(new BrandCommand.EqualityComparer()) { };
        private void timLED_Tick(object sender, EventArgs e)
        {
            foreach (BrandCommand.RevBoxData item in listledtime.Keys.ToArray())
            {
                //if (!issuspend)
                //{
                    //byte[] writearr = BrandCommand.GetState(item.Box, item.CardAddr);//继续查询
                    //serialPort1.Write(writearr, 0, writearr.Length);
                    //Writelog("发送串口命令:" + ByteToString(writearr), "ReadCommPort1", "SystemLog.txt");
                    //Thread.Sleep(500);
                    if (ConfigurationManager.AppSettings["LedTime"].Trim() != "")
                    {
                        if (listledtime[item].AddSeconds(Convert.ToDouble(ConfigurationManager.AppSettings["LedTime"].Trim()))<DateTime.Now)
                        {
                            byte[] w = new byte[6];
                            w[0] = 0xFE;
                            w[1] = item.CardAddr;
                            w[2] = item.Box;
                            w[3] = 0x03;
                            w[4] = (byte)(w[1] ^ w[2] ^ w[3]);
                            w[5] = 0xFF;
                            if (ListCode.ContainsKey(item))
                            {
                                ListCode[item].Send(w);
                                ListCode.Remove(item);
                                Writelog("TCP回复命令:" + ByteToString(w) + "," + item.CardAddr.ToString() + "," + item.Box.ToString(), "ReadCommPort1", "SystemLog.txt");
                            }
                            if (listledtime.ContainsKey(item))
                            {
                                listledtime.Remove(item);
                            }
                        }
                    }
                //}
            }
            //ArrayList akeys = new ArrayList(listledtime.Keys);
            //for (int i = listledtime.Count-1; i >-1; i--)
            //{
            //    listledtime.
            //}

        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.Visible = true;
                this.Visible = false;
            }
            else
            {
                notifyIcon1.Visible = true;
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void 显示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;
        }

       
    }
}
