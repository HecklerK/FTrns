using System;
using System.Collections.Generic;
using System.Text;

///////////////////////////////////////////////////////////////////////////////
//
//  www.interestprograms.ru /программы, игры и их исходные коды/
//  Протокол TCP. Часть 2. Отправки файлов и сообщений по сети. 
//
///////////////////////////////////////////////////////////////////////////////

namespace TcpSendFiles
{
    static class global
    {
        public const int MAXBUFFER = 1048576;
        public const int LENGTHHEADER = 9; // установленный размер главного заголовка
    }
}
