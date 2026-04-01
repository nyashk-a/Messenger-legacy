using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MessengerServer
{
    // jabaNetPassword

    /*
    * File.db
    *      Users                           (table)
    *          rID         (primary key)
    *          SUID        (UInt64)
    *          Name        (VRCHAR(40))
    *          Surname     (VRCHAR(40))
    *          Bio         (VRCHAR(120))
    *          Avatar      (VRCHAR(150))       это путь до файла
    *
    *      Messages                        (table)
    *          rID         (primary key)
    *          SUID        (UInt64)
    *          Time        (TIME)
    *          Owner       (UInt64)            это SUID отправителя
    *          Membership  (UInt64)            это SUID чата, в котором лежит месага
    *          Content     (nullable TEXT)
    *          attachedFile(VRCHAR(150))
    *      
    *      Chats                           (table)
    *          rID         (primary key)
    *          SUID        (UInt64)
    *          Name        (VRCHAR(40))
    *          Bio         (VRCHAR(120))
    *          Avatar      (VRCHAR(150))       это путь до файла (хранится в папке владельца)
    *      
    *      Participants                    (table)
    *          rID         (primary key)       не указываем здесь владельца чата, только участников : если это лс, то владелец тот, кто написал первым
    *          UserSUID    (UInt64)
    *          ChatSUID    (UInt64)
    *          UserRole    (Uint8)            банальным enum мы обозначим роли как цифры (вледлец-0, админ-1, участник-2, читатель-3) - что то вроде такого
    * 
    */

    public class User
    {
        [Key]
        public int RID { get; set; }
        public ulong SUID { get; set; }
        [MaxLength(40)]
        public string Name { get; set; }
        [MaxLength(40)]
        public string Surname { get; set; }
        [MaxLength(120)]
        public string Bio { get; set; }
        [MaxLength(150)]
        public string Avatar { get; set; }
    }

    public class Message
    {
        [Key]
        public int RID { get; set; }
        public ulong SUID { get; set; }
        public TimeOnly Time { get; set; }
        public ulong Owner { get; set; }
        public ulong Membership { get; set; }
        public string? Content { get; set; }
        [MaxLength(150)]
        public string? AttachedFile { get; set; }
    }

    public class Chat
    {
        [Key]
        public int RID { get; set; }
        public ulong SUID { get; set; }
        [MaxLength(40)]
        public string Name { get; set; }
        [MaxLength(120)]
        public string Bio { get; set; }
        [MaxLength(150)]
        public string Avatar { get; set; }
    }

    public class Participant
    {
        [Key]
        public int RID { get; set; }
        public ulong UserSUID { get; set; }
        public ulong ChatSUID { get; set; }
        public byte UserRole { get; set; }
    }
}
