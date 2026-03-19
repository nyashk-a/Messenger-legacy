using System;
using System.Collections.Generic;
using System.Threading;

namespace SharedTests.ClientSide
{
    internal static class ConsoleController
    {
        public static Action<string>? NetworkAcept;
        public static Action? kill;

        private static readonly List<string> messages = new List<string>();
        private static readonly Queue<string> incomingMessages = new Queue<string>();
        private static readonly object queueLock = new object();
        private static readonly AutoResetEvent messageEvent = new AutoResetEvent(false);

        private static string currentInput = "";
        private static int cursorPos = 0;
        private static int scrollOffset = 0;

        private static int consoleWidth;
        private static int consoleHeight;
        private static int historyAreaHeight;

        private static bool renderRequired = true;
        private static bool isRunning = true;

        public static void Run()
        {
            Initialize();

            while (isRunning)
            {
                // Ждём сигнал о новых сообщениях (до 10 мс)
                if (messageEvent.WaitOne(10))
                {
                    ProcessIncomingMessages();
                }

                // Обрабатываем все нажатия клавиш
                while (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    HandleKey(key);
                }

                // Если были изменения, перерисовываем
                if (renderRequired)
                {
                    Render();
                    renderRequired = false;
                }
            }

            Console.Clear();
            Console.CursorVisible = true;
        }

        public static void AddIncomingMessage(string message)
        {
            lock (queueLock)
            {
                incomingMessages.Enqueue(message);
            }
            messageEvent.Set();
        }

        private static void Initialize()
        {
            Console.Clear();
            Console.CursorVisible = false;
            UpdateDimensions();
        }

        private static void UpdateDimensions()
        {
            consoleWidth = Console.WindowWidth;
            consoleHeight = Console.WindowHeight;
            historyAreaHeight = Math.Max(0, consoleHeight - 2);
        }

        private static void ProcessIncomingMessages()
        {
            bool hasNewMessages = false;
            lock (queueLock)
            {
                while (incomingMessages.Count > 0)
                {
                    string msg = incomingMessages.Dequeue();
                    messages.Add(msg);
                    hasNewMessages = true;
                }
            }

            if (hasNewMessages)
            {
                renderRequired = true;
            }
        }

        private static void Render()
        {
            // Проверяем изменение размеров окна
            if (consoleWidth != Console.WindowWidth || consoleHeight != Console.WindowHeight)
            {
                UpdateDimensions();
                Console.Clear();
            }

            // Разделитель
            Console.SetCursorPosition(0, historyAreaHeight);
            Console.Write(new string('═', consoleWidth));

            // История сообщений
            int totalMessages = messages.Count;
            int firstVisibleIndex = Math.Max(0, totalMessages - historyAreaHeight - scrollOffset);
            int lastVisibleIndex = Math.Min(totalMessages, firstVisibleIndex + historyAreaHeight);

            for (int i = 0; i < historyAreaHeight; i++)
            {
                int messageIndex = firstVisibleIndex + i;
                Console.SetCursorPosition(0, i);

                if (messageIndex < lastVisibleIndex)
                {
                    string line = messages[messageIndex] + "\n";
                    if (line.Length > consoleWidth)
                        line = line.Substring(0, consoleWidth - 3) + "...";
                    Console.Write(line.PadRight(consoleWidth));
                }
                else
                {
                    Console.Write(new string(' ', consoleWidth));
                }
            }

            // Строка ввода
            Console.SetCursorPosition(0, historyAreaHeight + 1);
            string inputLine = "> " + currentInput;
            if (inputLine.Length > consoleWidth)
                inputLine = inputLine.Substring(0, consoleWidth);
            Console.Write(inputLine.PadRight(consoleWidth));

            // Курсор
            int cursorDisplayPos = Math.Min(2 + cursorPos, consoleWidth - 1);
            Console.SetCursorPosition(cursorDisplayPos, historyAreaHeight + 1);
            Console.CursorVisible = true;
        }

        private static void HandleKey(ConsoleKeyInfo key)
        {
            // Выход
            if (key.Key == ConsoleKey.Escape || (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.Q))
            {
                kill?.Invoke();
                isRunning = false;
                return;
            }

            // Enter: отправка сообщения
            if (key.Key == ConsoleKey.Enter)
            {
                if (!string.IsNullOrWhiteSpace(currentInput))
                {
                    // Отправляем через сеть
                    NetworkAcept?.Invoke(currentInput);
                    currentInput = "";
                    cursorPos = 0;
                    scrollOffset = 0;
                    renderRequired = true;
                }
                return;
            }

            // Прокрутка истории
            if (key.Key == ConsoleKey.UpArrow)
            {
                if (scrollOffset < messages.Count - historyAreaHeight)
                {
                    scrollOffset++;
                    renderRequired = true;
                }
                return;
            }
            if (key.Key == ConsoleKey.DownArrow)
            {
                if (scrollOffset > 0)
                {
                    scrollOffset--;
                    renderRequired = true;
                }
                return;
            }
            if (key.Key == ConsoleKey.PageUp)
            {
                int newOffset = Math.Min(messages.Count - historyAreaHeight, scrollOffset + historyAreaHeight);
                if (newOffset != scrollOffset)
                {
                    scrollOffset = newOffset;
                    renderRequired = true;
                }
                return;
            }
            if (key.Key == ConsoleKey.PageDown)
            {
                int newOffset = Math.Max(0, scrollOffset - historyAreaHeight);
                if (newOffset != scrollOffset)
                {
                    scrollOffset = newOffset;
                    renderRequired = true;
                }
                return;
            }
            if (key.Key == ConsoleKey.Home)
            {
                int newOffset = Math.Max(0, messages.Count - historyAreaHeight);
                if (newOffset != scrollOffset)
                {
                    scrollOffset = newOffset;
                    renderRequired = true;
                }
                return;
            }
            if (key.Key == ConsoleKey.End)
            {
                if (scrollOffset != 0)
                {
                    scrollOffset = 0;
                    renderRequired = true;
                }
                return;
            }

            // Редактирование строки ввода
            bool inputChanged = false;
            if (key.Key == ConsoleKey.LeftArrow)
            {
                if (cursorPos > 0)
                {
                    cursorPos--;
                    inputChanged = true;
                }
            }
            else if (key.Key == ConsoleKey.RightArrow)
            {
                if (cursorPos < currentInput.Length)
                {
                    cursorPos++;
                    inputChanged = true;
                }
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (cursorPos > 0)
                {
                    currentInput = currentInput.Remove(cursorPos - 1, 1);
                    cursorPos--;
                    inputChanged = true;
                }
            }
            else if (key.Key == ConsoleKey.Delete)
            {
                if (cursorPos < currentInput.Length)
                {
                    currentInput = currentInput.Remove(cursorPos, 1);
                    inputChanged = true;
                }
            }
            else if (key.Key == ConsoleKey.Home)
            {
                if (cursorPos != 0)
                {
                    cursorPos = 0;
                    inputChanged = true;
                }
            }
            else if (key.Key == ConsoleKey.End)
            {
                if (cursorPos != currentInput.Length)
                {
                    cursorPos = currentInput.Length;
                    inputChanged = true;
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                currentInput = currentInput.Insert(cursorPos, key.KeyChar.ToString());
                cursorPos++;
                inputChanged = true;
            }

            if (inputChanged)
                renderRequired = true;
        }
    }
}