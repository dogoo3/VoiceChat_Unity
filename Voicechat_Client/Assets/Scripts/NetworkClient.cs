using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;
using UnityEngine.InputSystem;
using TMPro;

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
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private GameObject roomUI;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isRunning = false;

    // 메인 스레드에서 UI 처리를 하기 위한 큐
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

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

            LoginPacket t_loginpacket = new LoginPacket();
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
    void ReceiveData()
    {
        byte[] buffer = new byte[4096];
        while (isRunning)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    if (bytes > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytes);
                        // Unity API는 메인 스레드에서만 접근 가능하므로 큐에 넣음
                        messageQueue.Enqueue(response);
                    }
                }
            }
            catch (Exception) { isRunning = false; }
        }
    }

    void Update()
    {
        // 큐에 쌓인 메시지가 있다면 처리
        while (messageQueue.TryDequeue(out string msg))
        {
            // Debug.Log($"[서버로부터 수신]: {msg}");
            
            // 필요하다면 여기서 JSON 파싱하여 로직 수행
            CommonPacket receivedData = JsonUtility.FromJson<CommonPacket>(msg);
            switch(receivedData.type)
            {
                case "nickname_check_result":
                    GET_NicknameCheck t_receivedData = JsonUtility.FromJson<GET_NicknameCheck>(msg);
                    if(t_receivedData.content) // 닉네임이 중복되지 않으면(true)
                    {
                        loginUI.SetActive(false);
                        lobbyUI.SetActive(true);
                        // 로비 화면으로 이동
                    }
                    else
                    {
                        tmp_errormessage.text = "중복된 닉네임입니다. 다른 닉네임을 입력하세요.";
                        // 닉네임 재입력 요구
                    }
                    break;
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