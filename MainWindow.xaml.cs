using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Threading;
using TcpSendFiles;
using System.IO;

namespace FTrns
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpModule _tcpmodule = new TcpModule();
        public string[,] ip = new string[256, 2];
        bool c = true;
        string myip = "0";

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
            bool add = false;
            string t;
            int i = 0;
            string ipn;
            for (; c <= j; ++c)
            {
                ipn = ipnum + c;
                t = PingCheck(ipn);
                if (t != null)
                {
                    for (int p = c; p <= j; p++)
                    {
                        if (ip[p, 0] == t)
                        {
                            add = true;
                            ip[p, 0] = ipn;
                            ip[p, 1] = ipn;
                            i++;
                            UpdList();
                            break;
                        }
                    }
                    if (!add)
                    {
                        for (int p = c; p <= j; p++)
                        {
                            if (ip[p, 0] == null)
                            {
                                add = true;
                                ip[p, 0] = ipn;
                                ip[p, 1] = ipn;
                                i++;
                                UpdList();
                                break;
                            }
                        }
                    }
                    add = false;
                }
                ipn = null;
            }
        }

        string Adapters()
        {
            string mip = "192.168.0.1";
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                GatewayIPAddressInformationCollection addresses = adapterProperties.GatewayAddresses;
                if (addresses.Count > 0)
                {
                    foreach (GatewayIPAddressInformation address in addresses)
                    {
                        if (address.Address.ToString().StartsWith("192.168."))
                        {
                            mip = address.Address.ToString();
                            break;
                        }
                        else mip = address.Address.ToString();
                    }
                }
            }
            return mip;
        }

        private void Check(object sender, EventArgs e)
        {
            if (c)
            {
                _tcpmodule.StartServer();
                c = false;
            }
            myip = Adapters();
            int li = myip.LastIndexOf('.');
            string ipnum = myip.Substring(0, li + 1);
            Task[] cip = new Task[5]
            {
                new Task(()=>Start(ipnum, 0, 51)),
                new Task(()=>Start(ipnum, 52, 102)),
                new Task(()=>Start(ipnum, 103, 153)),
                new Task(()=>Start(ipnum, 154, 204)),
                new Task(()=>Start(ipnum, 205, 255))
            };
            foreach (var item in cip) item.Start();
        }

        public void UpdList()
        {
            int sel;
            this.Dispatcher.Invoke(() =>
            {
                sel = list1.SelectedIndex;
                list1.Items.Clear();
                for (int i = 0; i < ip.Length / 2; i++)
                {
                    if (ip[i, 0] != null && ip[i, 1] != null) list1.Items.Add(ip[i, 0]);
                }
                list1.SelectedIndex = sel;
            });
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
            try
            {
                _tcpmodule.CloseSocket();
                c = true;
                int i = list1.SelectedIndex;
                _tcpmodule.ConnectClient(ip[i, 1]);
                Thread t = new Thread(() => _tcpmodule.SendData(ip[i, 0]));
                t.Start();
            }
            catch (Exception m)
            {
                if (m.Message == "Индекс находился вне границ массива.") System.Windows.MessageBox.Show("Выберите адресата");
                else System.Windows.MessageBox.Show("Не удалось отправить файл\n" + "Ошибка: " + m.Message);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            new AddIP(this).Show();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter("ip.txt", false, System.Text.Encoding.Default))
                {
                    for (int i = 0; i < ip.Length / 2; i++)
                    {
                        if (ip[i, 0] != null && ip[i, 1] != null && ip[i, 0] != ip[i, 1]) sw.WriteLine(ip[i, 0] + " " + ip[i, 1]);
                    }
                }
            }
            catch (Exception m)
            {
                System.Windows.MessageBox.Show(m.Message);
            }
            System.Windows.Application.Current.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (StreamReader sr = new StreamReader("ip.txt", System.Text.Encoding.Default))
                {
                    string line;
                    int i = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] splitLine = line.Split(' ');
                        ip[i, 0] = splitLine[0];
                        ip[i, 1] = splitLine[1];
                        i++;
                    }
                    UpdList();
                }
            }
            catch
            { 
            }
        }

        private void AllSend(object sender, RoutedEventArgs e)
        {
            _tcpmodule.CloseSocket();
            c = true;
            for (int i = 0; i < list1.Items.Count; i++)
            {
                _tcpmodule.ConnectClient(ip[i, 1]);
                Thread t = new Thread(() => _tcpmodule.SendData(ip[i, 0]));
                t.Start();
                t.Join();
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (list1.SelectedIndex != -1)
            {
                ip[list1.SelectedIndex, 0] = null;
                ip[list1.SelectedIndex, 1] = null;
                UpdList();
            }
        }
    }
}
