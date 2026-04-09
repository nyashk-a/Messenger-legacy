using System;



namespace Shared.Source.USC
{
    public enum MainCommand
    {
        CONNECT_CLIENT_SERVER_1_EN,
        CONNECT_CLIENT_SERVER_2_EE,
        CONNECT_CLIENT_SERVER_3_NE,

        CONNECT_CLIENT_CLIENT_1_EN,
        CONNECT_CLIENT_CLIENT_2_EE,
        CONNECT_CLIENT_CLIENT_3_NE,

        STD_AUTHENTICATION,
        TOKEN_AUTHENTICATION,

        HERE_IS_SYNC,
        I_RECEIVED_SYNC,

        GET_ACTIVE_CHATS,
        UPDATE_CHAT_HISTORY,

        MESSAGE_RECEIVED,
        MESSAGE_READ,

        SEND_MSG,
        SEND_PIC,
        SEND_FILE,
        SEND_MUSIC,
        DELETE_MSG,

        CHANGE_LOGIC,
        CHANGE_PASSWORD,
        DELETE_ACCOUNT
    }



    public enum SubCommand
    {
        NONE,

        SERVER_HERE_IS_NEW_ENCRYPTION_KEY,
        CLIENT_HERE_IS_NEW_ENCRYPTION_KEY,

        SWITCH_MY_SESSION_ID_TO_NEW_AND_SEND_IT_BACK
    }



    public enum Response
    {
        OK,
        OK_NOW_SYNCING,

        NOT_NOW_PLEASE_WAIT_FOR_SYNC,

        ERROR,
        ERROR_YOU_NEED_TO_REAUTHORISE
    }
}