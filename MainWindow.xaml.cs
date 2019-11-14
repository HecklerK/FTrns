using System;
using System.Threading.Tasks;
using System.Windows;
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
        TcpModule _tcpmodule = new TcpModule();
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
                    ip[i, 0] = t;
                    ip[i, 1] = t;
                    i++;
                    UpdList();
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
            return "192.168.0.1";
        }

        async void StartAsync(string ipn, int a, int b)
        {
            await Task.Run(() => Start(ipn, a, b));
        }

        private void Check(object sender, EventArgs e)
        {
            if (c)
            {
                _tcpmodule.StartServer();
                c = false;
            }
            string ipnum = Adapters().Substring(0, 10);
            StartAsync(ipnum, 0, 255);
        }

        private void UpdList()
        {
            this.Dispatcher.Invoke(() =>
            {
                list1.Items.Clear();
            });
                for (int i = 0; i < ip.Length / 2; i++)
            {
                this.Dispatcher.Invoke(() =>
                {
                    list1.Items.Add(ip[i, 0]);
                });
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _tcpmodule.SendFileName = dlg.FileName;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _tcpmodule.CloseSocket();
            c = true;
            _tcpmodule.ConnectClient(ip[list1.SelectedIndex, 0]);
            Thread t = new Thread(_tcpmodule.SendData);
            t.Start();
        }
    }
}
