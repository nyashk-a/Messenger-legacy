using Shared.Source;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Epsilon
{
    public static class PageController
    {
        private static ObservableCollection<MainPage.Contact> _contactsList = null;
        private static MainPage.Contact _user = null;

        public static void Init(MainPage.Contact usr, ObservableCollection<MainPage.Contact> chats)
        {
            if (_user == null || _contactsList == null)
            {
                _user = usr;
                _contactsList = chats;
            }
        }

        public static void ChangeName(string newName)
        {
            _user.Name = newName;
        }

        public static void ChangeSurname(string newSurname)
        {
            _user.surname = newSurname;
        }

        public static void ChangeBio(string newBio)
        {
            _user.bio = newBio;
        }

        public static void ChangeAvatar(ImageSource newAvatar)
        {
            _user.Avatar = newAvatar;
        }

        public static void AddContact(MainPage.Contact newContact)
        {
            _contactsList.Add(newContact);
        }

        public static bool RemoveContact(MainPage.Contact contact)
        {
            if (!_contactsList.Contains(contact)) return false;
            _contactsList.Remove(contact);
            return true;
        }

        public static bool SendMesage(MainPage.Contact contact, JN_Message message)
        {
            if (!_contactsList.Contains(contact)) return false;
            _contactsList.FirstOrDefault(contact).Messages.Add(message);
            return true;
        }

        public static bool RemoveMesage(MainPage.Contact contact, JN_Message message)
        {
            if (!_contactsList.Contains(contact)) return false;
            if (!_contactsList.FirstOrDefault(contact).Messages.Contains(message)) return false;
            _contactsList.FirstOrDefault(contact).Messages.Remove(message);
            return true;
        }

        public static bool ChangeMesage(MainPage.Contact contact, JN_Message oldMessage, JN_Message newMessage)
        {
            if (!_contactsList.Contains(contact)) return false;
            if (!_contactsList.FirstOrDefault(contact).Messages.Contains(oldMessage)) return false;
            //_contactsList.FirstOrDefault(contact).Messages.FirstOrDefault(oldMessage) = newMessage;
            return true;
        }
    }
}
