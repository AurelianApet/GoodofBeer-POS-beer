using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Errorcode : int
{
    ERR_VERSION = 0x00000000,

    ERR_UNKNOWN,
    ERR_INVALID_OPCODE,
    ERR_NOT_AUTH,
    ERR_SERVER_INTERNAL,
    ERR_INVALID_SERIAL,
    ERR_ALREADY_AUTH,

    ERR_VERSION_END
}
public enum Opcode : int
{
    OP_VERSION = 0x10000000,

    REQ_SERVER_INFO,
    RES_SERVER_INFO,
    REQ_PING,
    RES_PING,
    REQ_GET_CHANGED_OPCODES,
    RES_GET_CHANGED_OPCODES,
    REQ_AUTH_DEVICE,
    RES_AUTH_DEVICE,
    REQ_GET_TAG_INFO,
    RES_GET_TAG_INFO,
    REQ_GET_PRODUCT_INFO,
    RES_GET_PRODUCT_INFO,
    REQ_CHANGE_PRODUCT_INFO,
    RES_CHANGE_PRODUCT_INFO,
    REQ_GET_SHOP_INFO,
    RES_GET_SHOP_INFO,
    REQ_SET_SHOP_INFO,
    RES_SET_SHOP_INFO,
    REQ_GET_DEVICE_STATUS,
    RES_GET_DEVICE_STATUS,
    REQ_SET_DEVICE_STATUS,
    RES_SET_DEVICE_STATUS,
    REQ_SET_FLOWMETER_VALUE,
    RES_SET_FLOWMETER_VALUE,
    REQ_SET_PRODUCT_CONSUMPTION,
    RES_SET_PRODUCT_CONSUMPTION,
    REQ_GET_PRODUCT_QAUNTITY,
    RES_GET_PRODUCT_QAUNTITY,
    REQ_SET_MILLILITER_PER_PULSE,
    RES_SET_MILLILITER_PER_PULSE,
    ERROR_MESSAGE,

    OP_VERSION_END
}

public class Packet
{
    public Int32 length { get; private set; }
    public Int32 opcode { get; private set; }
    public Int64 msgid { get; private set; }
    public Int64 token { get; private set; }

    public Packet() { }

    public Packet(int length, int opcode, long msgid, long token, byte[] payload)
    {
        this.length = length;
        this.opcode = opcode;
        this.msgid = msgid;
        this.token = token;
    }

    public static int HeaderSize
    {
        get
        {
            return 24;
        }
    }
}
