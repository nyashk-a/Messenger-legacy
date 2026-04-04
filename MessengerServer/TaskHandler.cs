using System;
using System.Collections.Generic;
using System.Text;
using Shared.Source.NetDriver.AC;
using AVcontrol;
using Shared.Source.USC;
using System.ComponentModel.DataAnnotations;

namespace MessengerServer
{
    internal class TaskHandler
    {
        public async Task ProcessedTasks(Request req)
        {
            // здесь буквально прописывается все, что от нас потребует UCS
            // все возможности есть - бд настроена, нет-драйвер есть. остается только UCS

            switch () // req.message.content - спарсить и вытащить переменую
            {
                case MainCommand.CHANGE_LOGIC:
                    break;

                case MainCommand.HERE_IS_SYNC:
                    break;

                case MainCommand.I_RECEIVED_SYNC:
                    break;

                case MainCommand.MESSAGE_RECEIVED:
                    break;

                case MainCommand.MESSAGE_READ:
                    break;

                case MainCommand.CHANGE_PASSWORD:
                    break;

                case MainCommand.CONNECT_CLIENT_CLIENT_1_EN:
                    break;

                case MainCommand.CONNECT_CLIENT_CLIENT_2_EE:
                    break;

                case MainCommand.CONNECT_CLIENT_CLIENT_3_NE:
                    break;

                case MainCommand.CONNECT_CLIENT_SERVER_1_EN:
                    break;
            }
        }
    }
}
