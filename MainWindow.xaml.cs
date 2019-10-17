using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Runtime.InteropServices;
using System.Collections;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Threading;
using TcpSendFiles;

namespace FTrns
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[,] ip = new string[255, 2];
        bool c = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        string PingCheck(string A)
        {
            Ping ping = new Ping();
            PingReply pingReply = ping.Send(A, 500);
            if (pingReply.Status == IPStatus.Success)
            {
                return A;
            }
            return null;
        }

        void Start(string ipnum, int c, int j)
        {
            string t;
            int i = 0;
            string ipn;
            for (; c <= j; ++c)
            {
                ipn = ipnum + c;
                t = PingCheck(ipn);
                if (t != null)
                {
                    ip[i, 2] = t;
                    if (ip[i, 1] == null)
                    {

                    }
                    
                    
                    i++;
                }
                ipn = null;
            }
            Thread.Sleep(10);
        }

        string Adapters()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                GatewayIPAddressInformationCollection addresses = adapterProperties.GatewayAddresses;
                if (addresses.Count > 0)
                {
                    Console.WriteLine(adapter.Description);
                    foreach (GatewayIPAddressInformation address in addresses)
                    {
                        if (address.Address.ToString().StartsWith("192.168."))
                        {
                            return address.Address.ToString();
                        }
                    }
                }
            }
            return null;
        }

        async void StartAsync(string ipn, int a, int b)
        {
            await Task.Run(() => Start(ipn, a, b));
        }

        private void Check(object sender, EventArgs e)
        {
            if (c)
            {
                TcpModule ts = new TcpModule();
                ts.Start();
                c = false;
            }
            string ipnum = Adapters().Substring(0, 10);
            StartAsync(ipnum, 0, 255);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //_tcpmodule.SendFileName = dlg.FileName;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //_tcpmodule.ConnectClient((string)list1.SelectedItem);
            //Thread t = new Thread(_tcpmodule.SendData);
            //t.Start();
        }
    }
}
