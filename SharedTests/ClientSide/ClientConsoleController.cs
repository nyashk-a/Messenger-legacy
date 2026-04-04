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

        // Запоминаем предыдущие размеры окна для отслеживания изменений
        private static int lastWidth;
        private static int lastHeight;


        public static void Run()
        {
            Initialize();

            while (isRunning)
            {
                // Проверяем изменение размера окна (даже без событий)
                if (Console.WindowWidth != lastWidth || Console.WindowHeight != lastHeight)
                {
                    UpdateDimensions();
                    Console.Clear();
                    renderRequired = true;
                }

                if (messageEvent.WaitOne(10))
                {
                    ProcessIncomingMessages();
                }

                while (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    HandleKey(key);
                }

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
            lastWidth = consoleWidth;
            lastHeight = consoleHeight;
            // Оставляем минимум 2 строки: одна для разделителя, одна для ввода
            historyAreaHeight = Math.Max(1, consoleHeight - 2);
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
            // Скрываем курсор на время перерисовки (избегаем артефактов)
            Console.CursorVisible = false;

            // Разделительная линия (декор)
            Console.SetCursorPosition(0, historyAreaHeight);
            Console.Write(new string('═', consoleWidth));

            // Область истории сообщений с декоративным префиксом
            int totalMessages = messages.Count;
            int firstVisibleIndex = Math.Max(0, totalMessages - historyAreaHeight - scrollOffset);
            int lastVisibleIndex = Math.Min(totalMessages, firstVisibleIndex + historyAreaHeight);

            for (int i = 0; i < historyAreaHeight; i++)
            {
                int messageIndex = firstVisibleIndex + i;
                Console.SetCursorPosition(0, i);
                string line;

                if (messageIndex < lastVisibleIndex)
                {
                    string rawMessage = messages[messageIndex];
                    // Декор: добавляем символ "▶ " перед каждым сообщением
                    string decorated = "> " + rawMessage;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    line = decorated;
                    if (line.Length > consoleWidth)
                        line = line.Substring(0, consoleWidth - 3) + "...";
                    else
                        line = line.PadRight(consoleWidth);
                }
                else
                {
                    line = new string(' ', consoleWidth);
                    Console.ResetColor();
                }
                Console.Write(line);
                Console.ResetColor();
            }

            // Строка ввода с префиксом "> "
            Console.SetCursorPosition(0, historyAreaHeight + 1);
            string inputLine = "> " + currentInput;
            if (inputLine.Length > consoleWidth)
                inputLine = inputLine.Substring(0, consoleWidth);
            else
                inputLine = inputLine.PadRight(consoleWidth);
            Console.Write(inputLine);

            // Возвращаем курсор в позицию ввода
            int cursorDisplayPos = Math.Min(2 + cursorPos, consoleWidth - 1);
            Console.SetCursorPosition(cursorDisplayPos, historyAreaHeight + 1);
            Console.CursorVisible = true;
        }

        private static void HandleKey(ConsoleKeyInfo key)
        {
            // Выход по Escape или Ctrl+Q
            if (key.Key == ConsoleKey.Escape || (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.Q))
            {
                kill?.Invoke();
                isRunning = false;
                return;
            }

            // Enter – отправка сообщения
            if (key.Key == ConsoleKey.Enter)
            {
                if (!string.IsNullOrWhiteSpace(currentInput))
                {
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