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
using System.Windows.Shapes;
using FTrns;

namespace FTrns
{
    /// <summary>
    /// Логика взаимодействия для AddIP.xaml
    /// </summary>
    public partial class AddIP : Window
    {
        public MainWindow mainwindow;
        public AddIP(MainWindow _mainWindow)
        {
            InitializeComponent();
            mainwindow = _mainWindow;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Add(object sender, RoutedEventArgs e)
        {
            if (t_ip.Text != "Не правильный ip")
            {
                for (int i = 0; i < 256; i++)
                {
                    if (mainwindow.ip[i, 0] == null && mainwindow.ip[i, 1] == null)
                    {
                        mainwindow.ip[i, 0] = t_name.Text;
                        mainwindow.ip[i, 1] = t_ip.Text;
                        break;
                    }
                }
                mainwindow.UpdList();
                this.Close();
            }
        }

        private void t_ip_GotFocus(object sender, RoutedEventArgs e)
        {
            if (t_ip.Text == "IP - Адрес") t_ip.Text = "";
        }

        private void t_name_GotFocus(object sender, RoutedEventArgs e)
        {
            if (t_name.Text == "Название") t_name.Text = "";
        }

        private void t_name_LostFocus(object sender, RoutedEventArgs e)
        {
            if (t_name.Text == "") t_name.Text = "Название";
        }

        private void t_ip_LostFocus(object sender, RoutedEventArgs e)
        {
            string[] ip = t_ip.Text.Split('.');
            if (ip.Length < 4) t_ip.Text = "Не правильный ip";
            else
            {
                if (Convert.ToInt32(ip[0]) <= 0 || Convert.ToInt32(ip[0]) > 255) t_ip.Text = "Не правильный ip";
                if (Convert.ToInt32(ip[1]) <= 0 || Convert.ToInt32(ip[1]) > 255) t_ip.Text = "Не правильный ip";
                if (Convert.ToInt32(ip[2]) <= 0 || Convert.ToInt32(ip[2]) > 255) t_ip.Text = "Не правильный ip";
                if (Convert.ToInt32(ip[3]) <= 0 || Convert.ToInt32(ip[3]) > 255) t_ip.Text = "Не правильный ip";
            }
        }
    }
}
