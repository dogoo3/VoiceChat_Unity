using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;
using UnityEngine.InputSystem;

// JSON 직렬화를 위한 데이터 클래스
[Serializable]
public class PacketData
{
    public string type;
    public string content;
    public float x;
    public float y;
}

public class NetworkClient : MonoBehaviour
{
    [SerializeField] private string ip = "127.0.0.1";
    [SerializeField] private int port = 8080;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isRunning = false;

    // 메인 스레드에서 UI 처리를 하기 위한 큐
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    void Start()
    {
        ConnectToServer();
    }

    void ConnectToServer()
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

            // 입장 알림 보내기
            SendJson("login", "Unity Client Entered", transform.position);
        }
        catch (Exception e)
        {
            Debug.LogError($"접속 실패: {e.Message}");
        }
    }

    // 서버로 JSON 데이터 전송 함수
    public void SendJson(string type, string msg, Vector3 pos)
    {
        if (client == null || !client.Connected) return;

        PacketData packet = new PacketData();
        packet.type = type;
        packet.content = msg;
        packet.x = pos.x;
        packet.y = pos.y;

        string json = JsonUtility.ToJson(packet);
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
            PacketData receivedData = JsonUtility.FromJson<PacketData>(msg);
            Debug.Log(receivedData.content);
        }
    }

    
    // 테스트: 스페이스바를 누르면 현재 위치 전송
    void OnSpace()
    {
        Debug.Log($"스페이스바 입력");
        SendJson("move", "Moving", transform.position);
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (receiveThread != null) receiveThread.Abort();
        if (client != null) client.Close();
    }
}