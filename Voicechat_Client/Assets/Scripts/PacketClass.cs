// 서버와 데이터를 송수신하기 위한 약속된 패킷 클래스

using System;

[Serializable]
public struct CommonPacket<T> // content의 type이 뭐가 될지 모르니 Generic으로 설정
{
    public string type;
    public T content;
}

[Serializable]
public struct PacketData<T>
{
    public CommonPacket<T> common;
    public string timestamp;
}

[Serializable]
public struct LoginPacket<T>
{
    public CommonPacket<T> common;
}

[Serializable]
public struct GET_NicknameCheck
{
    public string type;
    public bool content;
    public string timestamp;
}

[Serializable]
public struct ArrayTestPacket<T>
{
    public CommonPacket<T> common;
}

[Serializable]
public struct GET_ArrayTestPacket
{
    public string type;
    public float[] content;
}