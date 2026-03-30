using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Shared.Source;
using AVcontrol;

namespace Epsilon
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        public class Contact : JN_Author, INotifyPropertyChanged
        {
            public Contact(string name, string surname, string bio, ulong suid, ImageSource avatar) : base(name, surname, bio, suid, avatar)
            {
                this.name = name;
                this.surname = surname;
                this.bio = bio;
                this.suid = suid;
                this.avatar = avatar;
            }

            public string Name
            {
                get => name;
                set
                {
                    name = value;
                    OnPropertyChanged();
                }
            }
            public string Surname
            {
                get => surname;
                set
                {
                    surname = value;
                    OnPropertyChanged();
                }
            }
            public ImageSource Avatar
            {
                get => avatar;
                set
                {
                    avatar = value;
                    OnPropertyChanged();
                }
            }

            private ObservableCollection<JN_Message> _messages = new();

            public ObservableCollection<JN_Message> Messages
            {
                get => _messages;
                set
                {
                    _messages = value;
                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public class Message : JN_Message, INotifyPropertyChanged
        {
            public string StringSentTime
            {
                get => SentTime.ToStringTime();
            }
            public Message(DateTime4b sentTime, string message, ulong authorSUID) : base(sentTime, message, authorSUID)
            {
                this.sentTime = sentTime;
                this.message = message;
                this.authorSUID = authorSUID;
            }

            public string Text
            {
                get => message;
                set
                {
                    message = value;
                    OnPropertyChanged();
                }
            }

            public DateTime4b SentTime
            {
                get => sentTime;
                set
                {
                    sentTime.ToStringTime();
                    sentTime = value;
                    OnPropertyChanged();
                }
            }


            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ObservableCollection<Contact> Contacts { get; } = new();

        private Contact _actualChat = null;
        public Contact ActualChat
        {
            get => _actualChat;
            set
            {
                if (_actualChat != value)
                {
                    _actualChat = value;
                    OnPropertyChanged(nameof(ActualChat));
                }
            }
        }


        private Contact _user = null;

        public Contact User
        {
            get => _user;
            set
            {
                if (_user != value)
                {
                    _user = value;
                    OnPropertyChanged(nameof(User));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainPage()
        {
            InitializeComponent();
            PageController.Init(User, Contacts);

            BindingContext = this;

            // потом убить ->


            User = new("Frogges", "-Name", "", 12, ImageSource.FromFile("C:\\Users\\suzi\\Pictures\\Avatars\\photo_2026-01-17_23-19-08.jpg"));

            Contacts.Add(new Contact("aboba", "", "", 12, ImageSource.FromFile("C:\\Users\\suzi\\Pictures\\Avatars\\photo_2026-01-17_23-19-06.jpg")));
            Contacts.Add(new Contact("aboba1", "", "", 12, ImageSource.FromFile("C:\\Users\\suzi\\Pictures\\Avatars\\photo_2026-01-17_23-19-08.jpg")));
            Contacts.Add(new Contact("aboba2", "", "", 12, ImageSource.FromFile("C:\\Users\\suzi\\Pictures\\Avatars\\photo_2026-01-17_23-19-05.jpg")));

            Contacts[0].Messages.Add(new Message(DateTime4b.Now, "привет!", 12));
            Contacts[0].Messages.Add(new Message(DateTime4b.Now, "пашел нахуй!", 12));
            Contacts[0].Messages.Add(new Message(DateTime4b.Now, "и тебе не хворать", 12));
            Contacts[0].Messages.Add(new Message(DateTime4b.Now, "О, сарказм. Ценю. Извини, просто день говно. Только что отбил бампер у своего самосвала, пока объезжал идиота на легковушке, который пялился в телефон на кольцевой. Весь рейс насмарку.", 12));
            Contacts[0].Messages.Add(new Message(DateTime4b.Now, "Боже, да это же просто жуть какая-то! Нет, я серьезно, я представляю этот стресс! Ты представляешь, сколько теперь бумажной волокиты? Страховка, акт, возможно, оценка ущерба, простой транспортного средства, срыв графика поставок... Это же цепная реакция! Кстати, на каком именно грузовике работаешь? У нас в семье дядя дальнобойщиком был, так я с детства в Scania, MAN и Volvo чуть ли не разбиралась. Он мне все эти истории рассказывал про рейсы в Уфу или в Питер, как ночью на заправках кофе пьют, как видят самые невероятные рассветы над трассой, а иногда и самые глупые ДТП, когда кто-то пытается втиснуться между двумя фурами, не понимая физики их тормозного пути. Кстати, о физике... помнится, он объяснял, почему нельзя cut-off делать перед фурой. Это ж не просто «ой, простите». Масса-то какая! Инерция! Представь себе стального зверя весом под 40 тонн, мчащегося со скоростью 90 км/ч. Остановить его – не то что легковушку. Это тебе не урок в школе, это реальная, грубая физика на асфальте. 🚛💨☕", 12));
            Contacts[0].Messages.Add(new Message(DateTime4b.Now, "Ты что, энциклопедия на колесах? Работаю на Volvo FH, седельный. И да, про инерцию я тебе лучше любого учебника расскажу, особенно когда гололед. Но сегодня не про это. После этого инцидента поехал в техцентр, а там очередь. Сижу, жду, и тут начинаю думать... а вообще, какая сложная и на самом деле красивая эта вся система грузоперевозок. Ну вот смотри: стоит где-нибудь в Подмосковье завод, которому нужны детали из Германии. Их грузят в контейнер, контейнер – на шасси, шасси – на паром, паром – через Балтику, потом снова на шасси, и вот он, мой Volvo, уже тянет этот контейнер по МКАДу. Это же как кровеносная система экономики! Артерии – магистрали, капилляры – разъезды по городам. И мы, водители, как эритроциты, тащим кислород-грузы к клеткам-заводам. Понимаешь? А все из-за какого-то клоуна в хэтчбеке, который смотрел тиктоки. Вся эта грандиозная схема дала сбой в одном звене. И ладно бы я один, таких звеньев – тысячи на дороге в любой момент. И каждое – это человек, металл, солярка, законы, погода, эмоции. Забавно, когда об этом думаешь с высоты. Ну или с высоты водительского кресла, пока стоишь в очереди на покраску бампера.", 12));
        }
        private void OnItemTapped(object sender, EventArgs e)
        {
            if (sender is Border border && border.GestureRecognizers[0] is TapGestureRecognizer tap)
            {
                if (tap.CommandParameter is Contact contact)
                {
                    ActualChat = contact;
                }
            }
        }
        private void SubmitText(object sender, EventArgs e)
        {
            if (ActualChat == null) return;

            Console.WriteLine(MessageInputField.Text);
            ActualChat.Messages.Add(new Message(DateTime4b.Now, MessageInputField.Text, 12));
            MessageInputField.Text = string.Empty;
        }
    }
}
