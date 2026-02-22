using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

// JSON 직렬화를 위한 데이터 클래스
public class NetworkClient : MonoBehaviour
{
    public static NetworkClient instance;

    [SerializeField] private string ip = "127.0.0.1";
    [SerializeField] private int port = 8080;
    [Space(50)]
    [SerializeField] private TMP_Text tmp_errormessage;
    [Space(50)]
    [SerializeField] private GameObject loginUI;
    [SerializeField] private RoomManager roomUI;
    [SerializeField] private AudioSource source;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isRunning = false;

    // 메인 스레드에서 UI 처리를 하기 위한 큐
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    // 수신 음성 프레임 데이터 처리를 위한 큐와 리스트 선언
    private ConcurrentQueue<GET_AnotherSoundFramePacket> audioQueue = new ConcurrentQueue<GET_AnotherSoundFramePacket>();
    private List<byte> receiveBuffer = new List<byte>();

    private void Awake() {
        instance = this;
    }

    public void ConnectToServer(string p_nickname)
    {
        try
        {
            client = new TcpClient(ip, port);
            stream = client.GetStream();
            isRunning = true;
            Debug.Log("서버 접속 성공");

            // 수신 전용 스레드 시작
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            LoginPacket<string> t_loginpacket = new LoginPacket<string>();
            t_loginpacket.common.type = "login";
            t_loginpacket.common.content = p_nickname;

            // 입장 요청 보내기(닉네임과 함께)
            SendJson(ref t_loginpacket);
        }
        catch (Exception e)
        {
            if(e.Message == "대상 컴퓨터에서 연결을 거부했으므로 연결하지 못했습니다.")
            {
                // 여기에 TMP 오브젝트 연동해야 함
            }
        }
    }

    // 서버로 JSON 데이터 전송 함수
    // public void SendJson(string type, string msg, Vector3 pos)
    // {
    //     if (client == null || !client.Connected) return;

    //     PacketData packet = new PacketData();
    //     packet.common.type = type;
    //     packet.common.content = msg;
    //     packet.x = pos.x;
    //     packet.y = pos.y;

    //     string json = JsonUtility.ToJson(packet);
    //     byte[] data = Encoding.UTF8.GetBytes(json);
    //     stream.Write(data, 0, data.Length);
    // }

    public void SendJson<T>(ref T p_packet)
    {
        if(client == null || !client.Connected) return;

        string json = JsonUtility.ToJson(p_packet);
        byte[] data = Encoding.UTF8.GetBytes(json);
        stream.Write(data, 0, data.Length);
    }

    // 데이터 수신 (별도 스레드)
    // void ReceiveData()
    // {
    //     byte[] buffer = new byte[8192];
    //     while (isRunning)
    //     {
    //         try
    //         {
    //             if (stream.DataAvailable)
    //             {
    //                 int bytes = stream.Read(buffer, 0, buffer.Length);
    //                 if (bytes > 0)
    //                 {
    //                     string response = Encoding.UTF8.GetString(buffer, 0, bytes);
    //                     // Unity API는 메인 스레드에서만 접근 가능하므로 큐에 넣음
    //                     messageQueue.Enqueue(response);
    //                 }
    //             }
    //         }
    //         catch (Exception) { isRunning = false; }
    //     }
    // }

    void ReceiveData()
    {
        byte[] buffer = new byte[8192];
        while (isRunning)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    if (bytes > 0)
                    {
                        // 1. 들어온 바이트를 모두 버퍼에 밀어넣음
                        for (int i = 0; i < bytes; i++) receiveBuffer.Add(buffer[i]);

                        // 2. 최소 4바이트(Payload 크기 정보)가 모였는지 확인
                        while (receiveBuffer.Count >= 4)
                        {
                            int payloadSize = BitConverter.ToInt32(receiveBuffer.ToArray(), 0);
                            int totalPacketSize = 4 + payloadSize;

                            // 쓰레기 데이터 방어 로직 (선택사항이나 권장)
                            if (payloadSize <= 0 || payloadSize > 5000000) 
                            {
                                receiveBuffer.Clear();
                                break;
                            }

                            // 3. 전체 패킷이 도착할 때까지 대기
                            if (receiveBuffer.Count >= totalPacketSize)
                            {
                                // 헤더(4바이트)를 제외한 실제 데이터(Payload)만 추출
                                byte[] payloadData = receiveBuffer.GetRange(4, payloadSize).ToArray();
                                receiveBuffer.RemoveRange(0, totalPacketSize); // 처리한 부분 버퍼에서 삭제

                                // --- [데이터 분기 처리] ---
                                // JSON은 항상 '{' (아스키코드 123)으로 시작한다는 점을 이용해 구분
                                if (payloadData.Length > 0 && payloadData[0] == 123) 
                                {
                                    // [JSON 데이터 처리]
                                    string jsonMsg = Encoding.UTF8.GetString(payloadData);
                                    messageQueue.Enqueue(jsonMsg);
                                }
                                else 
                                {
                                    // [바이너리(Audio) 데이터 처리]
                                    int offset = 0;
                                    
                                    // Type 문자열 해체
                                    int typeLen = BitConverter.ToInt32(payloadData, offset); offset += 4;
                                    string typeStr = Encoding.UTF8.GetString(payloadData, offset, typeLen); offset += typeLen;

                                    if (typeStr == "anotheraudio")
                                    {
                                        int channel = BitConverter.ToInt32(payloadData, offset); offset += 4;
                                        int floatCount = BitConverter.ToInt32(payloadData, offset); offset += 4;
                                        
                                        // Float 배열 통째로 복사 (매우 빠름)
                                        float[] floatArray = new float[floatCount];
                                        Buffer.BlockCopy(payloadData, offset, floatArray, 0, floatCount * 4);

                                        // 큐에 담기
                                        GET_AnotherSoundFramePacket audioPacket = new GET_AnotherSoundFramePacket();
                                        audioPacket.type = typeStr;
                                        audioPacket.channel = channel;
                                        audioPacket.frame_audio = floatArray;
                                        
                                        audioQueue.Enqueue(audioPacket);
                                    }
                                }
                            }
                            else
                            {
                                break; // 데이터가 아직 덜 왔으면 다음 stream.Read를 기다림
                            }
                        }
                    }
                }
            }
            catch (Exception e) 
            { 
                Debug.LogWarning($"수신 스레드 종료: {e.Message}");
                isRunning = false; 
            }
        }
    }

    void Update()
    {
        // 큐에 쌓인 메시지가 있다면 처리
        while (messageQueue.TryDequeue(out string msg))
        {
            // Debug.Log($"[서버로부터 수신]: {msg}");
            
            // 필요하다면 여기서 JSON 파싱하여 로직 수행
            CommonPacket<string> receivedData = JsonUtility.FromJson<CommonPacket<string>>(msg);
            switch(receivedData.type)
            {
                case "nickname_check_result":
                    GET_NicknameCheck t_receivedData = JsonUtility.FromJson<GET_NicknameCheck>(msg);
                    if(t_receivedData.content) // 닉네임이 중복되지 않으면(true)
                    {
                        loginUI.SetActive(false);
                        roomUI.gameObject.SetActive(true);

                        // 몇 번째 입장인지 받아옴. 1~5가 받아와지고, 1이 첫번째.
                        Debug.Log(t_receivedData.users_name.Length);
                        // 방으로 이동하며, 방 안의 닉네임 설정
                        roomUI.InitializeUserName(t_receivedData.mynickname, t_receivedData.users_name);
                    }
                    else
                    {
                        tmp_errormessage.text = "중복된 닉네임입니다. 다른 닉네임을 입력하세요.";
                        // 닉네임 재입력 요구
                    }
                    break;
                case "array_test_result":
                    GET_ArrayTestPacket t_asdf = JsonUtility.FromJson<GET_ArrayTestPacket>(msg);
                    for(int i=0;i<t_asdf.content.Length;i++)
                        Debug.Log(t_asdf.content[i]);
                    Debug.Log(t_asdf.type);
                    break;
                case "add_user":
                    GET_AddUserPacket t_adduserData = JsonUtility.FromJson<GET_AddUserPacket>(msg);
                    roomUI.AddUser(t_adduserData.add_username);
                    break;
                case "myexit": // 내가 나갈 때
                    roomUI.ResetRoomSetting();
                    roomUI.gameObject.SetActive(false);
                    loginUI.SetActive(true);
                    break;
                case "anotherexit": // 다른 사람이 나갈 때
                    GET_AnotherUserExitInfoPacket t_content = JsonUtility.FromJson<GET_AnotherUserExitInfoPacket>(msg);
                    roomUI.ExitAnotherUser(t_content.exit_nickname);
                    break;
                case "anotheraudio": // 다른 사람의 오디오를 수신할 때
                    GET_AnotherSoundFramePacket t_soundframe = JsonUtility.FromJson<GET_AnotherSoundFramePacket>(msg);
                    source.clip = AudioClip.Create("Real_time", t_soundframe.frame_audio.Length, t_soundframe.channel, 44100, false);
                    source.spatialBlend = 0; //2D sound
                    source.clip.SetData(t_soundframe.frame_audio, 0);
                    if (!this.source.isPlaying)
                    {
                        this.source.Play();
                    }
                    break;
            }
        }

        // 메시지 큐에 쌓인 바이너리 오디오 패킷 처리 (새로 추가된 부분)
        while (audioQueue.TryDequeue(out GET_AnotherSoundFramePacket t_soundframe))
        {
            // 오디오 클립 생성 및 재생
            source.clip = AudioClip.Create("Real_time", t_soundframe.frame_audio.Length, t_soundframe.channel, 44100, false);
            source.spatialBlend = 0; // 2D sound
            source.clip.SetData(t_soundframe.frame_audio, 0);
            
            if (!this.source.isPlaying)
            {
                this.source.Play();
            }
        }
    }

    
    // 테스트: 스페이스바를 누르면 현재 위치 전송
    void OnSpace()
    {
        
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (receiveThread != null) receiveThread.Abort();
        if (client != null) client.Close();
    }
}