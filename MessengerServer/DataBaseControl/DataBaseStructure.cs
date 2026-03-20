using System;
using System.Collections.Generic;
using System.Text;
using static MessengerServer.SQLiteHandler;

namespace MessengerServer.DataBaseControl
{
    internal class DataBaseStructure
    {
        /*
         * File.db
         *      Users                           (table)
         *          ID          (primary key)
         *          SUID        (UInt64)
         *          Name        (VRCHAR(40))
         *          Surname     (VRCHAR(40))
         *          Bio         (VRCHAR(120))
         *          Avatar      (VRCHAR(150)        это путь до файла
         *
         *      Messages                        (table)
         *          ID          (primari key)
         *          SUID        (UInt64)
         *          Time        (TIME)
         *          Owner       (UInt64)            это SUID отправителя
         *          Membership  (UInt64)            это SUID чата, в котором лежит месага
         *          Content     (TEXT)
         *      
         *      Chats                           (table)
         *          ID          (primari key)
         *          SUID        (UInt64)
         *          Owner       (UInt64)            SUID владельца
         *          Name        (VRCHAR(40))
         *          Bio         (VRCHAR(120))
         *          Avatar      (VRCHAR(150))       это путь до файла (хранится в папке владельца)
         *      
         *      Dependencies                    (table)
         *          ID          (primari key)       не указываем здесь владельца чата, только участников : если это лс, то владелец тот, кто написал первым
         *          UserSUID    (UInt64)
         *          ChatSUID    (UInt64)
         * 
         */
    }
}
