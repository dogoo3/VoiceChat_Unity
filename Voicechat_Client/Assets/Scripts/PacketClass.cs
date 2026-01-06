// 서버와 데이터를 송수신하기 위한 약속된 패킷 클래스

using System;

[Serializable]
public struct CommonPacket
{
    public string type;
    public string content;
}

[Serializable]
public struct PacketData
{
    public CommonPacket common;
    public string timestamp;
}

[Serializable]
public struct LoginPacket
{
    public CommonPacket common;
}

[Serializable]
public struct GET_NicknameCheck
{
    public string type;
    public bool content;
    public string timestamp;
}