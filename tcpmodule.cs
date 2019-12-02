using System;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Text;

namespace TcpSendFiles
{
    /// <summary>
    /// Класс для передачи десериализированного контейнера при 
    /// возникновении события получения сетевых данных.
    /// </summary>
    class ReceiveEventArgs : EventArgs
    {
        private SendInfo _sendinfo;

        public ReceiveEventArgs(SendInfo sendinfo)
        {
            _sendinfo = sendinfo;
        }

        public SendInfo sendInfo
        {
            get { return _sendinfo; }
        }
    }

    
    /// <summary>
    /// Класс способный выступать в роли сервера или клиента в TCP соединении.
    /// Отправляет и получает по сети файлы и текстовые сообщения.
    /// </summary>
    class TcpModule
    {
        
        #region Определение событий сетевого модуля

        // Типы делегатов для обработки событий в паре с соответствующим объектом события.

        // Обработчики события акцептирования (принятия клиентов) прослушивающим сокетом
        public delegate void AcceptEventHandler(object sender);
        public event AcceptEventHandler Accept; 
        
        // Обработчики события подключения клиента к серверу
        public delegate void ConnectedEventHandler(object sender, string result);
        public event ConnectedEventHandler Connected;

        // Обработчики события отключения конечных точек (клиентов или сервера)
        public delegate void DisconnectedEventHandler(object sender, string result);
        public event DisconnectedEventHandler Disconnected;

        // Обработчики события извлечения данных 
        public delegate void ReceiveEventHandler(object sender, ReceiveEventArgs e);
        public event ReceiveEventHandler Receive;

        #endregion


        #region Объявления членов класса

        // Родительская форма необходима для визуальной информации 
        // о внутреннем состоянии и событиях работы сетвого модуля.

        // Прослушивающий сокет для работы модуля в режиме сервера TCP
        private TcpListener _tcpListener;

        // Удобный контейнер для подключенного клиента.
        private TcpClientData _tcpClient;

        /// <summary>
        /// Возможные режимы работы TCP модуля
        /// </summary>
        public enum Mode { indeterminately, Server, Client};

        /// <summary>
        /// Режим работы TCP модуля
        /// </summary>
        public Mode modeNetwork;

        #endregion


        #region Интерфейсная часть TCP модуля

        /// <summary>
        /// Запускает сервер, прослушивающий все IP адреса, и одновременно
        /// метод асинхронного принятия (акцептирования) клиентов.
        /// </summary>
        public void StartServer()
        {
            if (modeNetwork == Mode.indeterminately)
            {
                try
                {
                    _tcpListener = new TcpListener(IPAddress.Any, 15000);
                    _tcpListener.Start();
                    _tcpListener.BeginAcceptTcpClient(AcceptCallback, _tcpListener);
                    modeNetwork = Mode.Server;
                }
                catch (Exception e)
                {
                    _tcpListener = null;
                    MessageBox.Show(e.Message);
                }
            }
            else
            {
                
            }
        }


        /// <summary>
        /// Остановка сервера
        /// </summary>
        public void StopServer()
        {
            if (modeNetwork == Mode.Server)
            {
                modeNetwork = Mode.indeterminately;
                _tcpListener.Stop();
                _tcpListener = null;
                DeleteClient(_tcpClient);
            }
        }


        /// <summary>
        /// Попытка асинхронного подключения клиента к серверу
        /// </summary>
        /// <param name="ipserver">IP адрес сервера</param>
        public void ConnectClient(string ipserver)
        {
            if (modeNetwork == Mode.indeterminately)
            {
                _tcpClient = new TcpClientData();
                _tcpClient.tcpClient.BeginConnect(IPAddress.Parse(ipserver), 15000, new AsyncCallback(ConnectCallback), _tcpClient);
                modeNetwork = Mode.Client;
            }
        }


        /// <summary>
        /// Отключение клиента от сервера
        /// </summary>
        public void DisconnectClient()
        {
            if (modeNetwork == Mode.Client)
            {
                modeNetwork = Mode.indeterminately;
                DeleteClient(_tcpClient);
            }
        }


        /// <summary>
        /// Завершение работы подключенного клиента
        /// </summary>
        private void DeleteClient(TcpClientData mtc)
        {
            if (mtc != null && mtc.tcpClient.Connected == true)
            {
                mtc.tcpClient.GetStream().Close(); // по настоянию MSDN закрываем поток отдельно у клиента
                mtc.tcpClient.Close(); // затем закрываем самого клиента
            }
        }
 

        /// <summary>
        /// Метод упрощенного создания заголовка с информацией о размере данных отправляемых по сети.
        /// </summary>
        /// <param name="length">длина данных подготовленных для отправки по сети</param>
        /// <returns>возращает байтовый массив заголовка</returns>
        private byte[] GetHeader(int length)
        {
            string header = length.ToString();
            if (header.Length < 9)
            {
                string zeros = null;
                for (int i = 0; i < (9 - header.Length); i++)
                {
                    zeros += "0";
                }
                header = zeros + header;
            }

            byte[] byteheader = Encoding.Default.GetBytes(header);
            return byteheader;
        }



        public string SendFileName = null;
        public void SendData(string ipserver)
        {
            SendInfo si = new SendInfo();
            if (String.IsNullOrEmpty(SendFileName) == true) return;
            if (SendFileName != null)
            {
                FileInfo fi = new FileInfo(SendFileName);
                if (fi.Exists == true)
                {
                    si.filesize = (int)fi.Length;
                    si.filename = fi.Name;
                }
                fi = null;
            }
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, si);
            ms.Position = 0;
            byte[] infobuffer = new byte[ms.Length];
            int r = ms.Read(infobuffer, 0, infobuffer.Length);
            ms.Close();
            byte[] header = GetHeader(infobuffer.Length);
            byte[] total = new byte[header.Length + infobuffer.Length + si.filesize];
            Buffer.BlockCopy(header, 0, total, 0, header.Length);
            Buffer.BlockCopy(infobuffer, 0, total, header.Length, infobuffer.Length);
            // Если путь файла указан, добавим его содержимое в отправляемый массив байтов
            if (si.filesize > 0)
            {
                FileStream fs = new FileStream(SendFileName, FileMode.Open, FileAccess.Read);
                fs.Read(total, header.Length + infobuffer.Length, si.filesize);
                fs.Close();
                fs = null;
            }
            // Отправим данные подключенным клиентам
            try
            {
                NetworkStream ns = _tcpClient.tcpClient.GetStream();
                ns.Write(total, 0, total.Length);
                header = null;
                infobuffer = null;
                total = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                // Подтверждение успешной отправки
                MessageBox.Show("Файл отправился на IP: " + ipserver);
            }
            catch
            {
                MessageBox.Show("Не удалось отправить файл " + ipserver);
            }
            // Так как данный метод вызывается в отдельном потоке рациональней использовать синхронный метод отправки
            // Обнулим все ссылки на многобайтные объекты и попробуем очистить память
            StartServer();
        }

        public void CloseSocket()
        {
            StopServer();
            DisconnectClient();
        }

        private void SoundError()
        {
            Console.Beep(3000, 30);
            Console.Beep(1000, 30);
        }

        #endregion


        #region Асинхронные методы сетевой работы TCP модуля


        /// <summary>
        /// Обратный метод завершения принятия клиентов
        /// </summary>
        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                if (modeNetwork == Mode.indeterminately) return;
                TcpListener listener = (TcpListener)ar.AsyncState;
                _tcpClient = new TcpClientData();
                _tcpClient.tcpClient = listener.EndAcceptTcpClient(ar);
                // Немедленно запускаем асинхронный метод извлечения сетевых данных
                // для акцептированного TCP клиента
                NetworkStream ns = _tcpClient.tcpClient.GetStream();
                _tcpClient.buffer = new byte[global.LENGTHHEADER];
                ns.BeginRead(_tcpClient.buffer, 0, _tcpClient.buffer.Length, new AsyncCallback(ReadCallback), _tcpClient);
                // Продолжаем ждать запросы на подключение
                listener.BeginAcceptTcpClient(AcceptCallback, listener);
                // Активация события успешного подключения клиента
                if (Accept != null)
                {
                    Accept.BeginInvoke(this, null, null);
                }
            }
            catch
            {
                // Обработка исключительных ошибок возникших при акцептирования клиента.
            }
        }

        public void ConnectCallback(IAsyncResult ar)
        {
            string result = "Подключение успешно!";
            try
            {
                // Получаем подключенного клиента
                TcpClientData myTcpClient = (TcpClientData)ar.AsyncState;
                NetworkStream ns = myTcpClient.tcpClient.GetStream();
                myTcpClient.tcpClient.EndConnect(ar);

                // Запускаем асинхронный метод чтения сетевых данных для подключенного TCP клиента
                myTcpClient.buffer = new byte[global.LENGTHHEADER];
                ns.BeginRead(myTcpClient.buffer, 0, myTcpClient.buffer.Length, new AsyncCallback(ReadCallback), myTcpClient);

            }
            catch
            {
                //MessageBox.Show(e.Message);
                // Обработка ошибок подключения
                DisconnectClient();
                result = "Подключение провалено!";
            }

            // Активация события успешного или неуспешного подключения к серверу,
            // здесь серверу можно отослать ознакомительные данные о себе (например, имя клиента)
            if (Connected != null)
                Connected.BeginInvoke(this, result, null, null);
        }

        /// <summary>
        /// Метод асинхронно вызываемый при наличие данных в буферах приема.
        /// </summary>
        public void ReadCallback(IAsyncResult ar)
        {
            if (modeNetwork == Mode.indeterminately) return;
            TcpClientData myTcpClient = (TcpClientData)ar.AsyncState;
            try
            {
                NetworkStream ns = myTcpClient.tcpClient.GetStream();
                int r = ns.EndRead(ar);
                if (r > 0)
                {
                    // Из главного заголовка получим размер массива байтов информационного объекта
                    string header = Encoding.Default.GetString(myTcpClient.buffer);
                    int leninfo = int.Parse(header);
                    // Получим и десериализуем объект с подробной информацией о содержании получаемого сетевого пакета
                    MemoryStream ms = new MemoryStream(leninfo);
                    byte[] temp = new byte[leninfo];
                    r = ns.Read(temp, 0, temp.Length);
                    ms.Write(temp, 0, r);
                    BinaryFormatter bf = new BinaryFormatter();
                    ms.Position = 0;
                    SendInfo sc = (SendInfo)bf.Deserialize(ms);
                    ms.Close();
                    if (sc.filesize > 0)
                    {
                        // Создадим файл на основе полученной информации и массива байтов следующих за объектом информации
                        sc.filename = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\" + sc.filename;
                        FileStream fs = new FileStream(sc.filename, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, sc.filesize);
                        do
                        {
                            temp = new byte[global.MAXBUFFER];
                            r = ns.Read(temp, 0, temp.Length);
                            // Записываем строго столько байтов сколько прочтено методом Read()
                            fs.Write(temp, 0, r);
                            // Как только получены все байты файла, останавливаем цикл,
                            // иначе он заблокируется в ожидании новых сетевых данных
                            if (fs.Length == sc.filesize)
                            {
                                fs.Close();
                                fs = null;
                                break;
                            }
                        }
                        while (r > 0);
                        temp = null;
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    if (Receive != null) Receive(this, new ReceiveEventArgs(sc));
                    myTcpClient.buffer = new byte[global.LENGTHHEADER];
                    ns.BeginRead(myTcpClient.buffer, 0, myTcpClient.buffer.Length, new AsyncCallback(ReadCallback), myTcpClient);
                    StartServer();
                }
                else
                {
                    DeleteClient(myTcpClient);
                    // Событие клиент отключился
                    if (Disconnected != null) Disconnected.BeginInvoke(this, "Клиент отключился!", null, null);
                }
            }
            catch
            {

                DeleteClient(myTcpClient);
                // Событие клиент отключился
                if (Disconnected != null) Disconnected.BeginInvoke(this, "Клиент отключился аварийно!", null, null);
                SoundError();
            }
        }
        #endregion
    }

    class TcpClientData
    {
        public TcpClient tcpClient = new TcpClient();

        // Буфер для чтения и записи данных сетевого потока
        public byte[] buffer = null;

        public TcpClientData()
        {
            tcpClient.ReceiveBufferSize = global.MAXBUFFER;
        }
    }

    [Serializable]
    class SendInfo
    {
        public string filename;
        public int filesize;
    }
}
