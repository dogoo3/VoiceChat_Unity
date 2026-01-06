#pragma once
#include <winsock2.h>
#include <ws2tcpip.h>
#include <map>
#include <thread>
#include <mutex>
#include <string>
#include <vector>

#pragma comment(lib, "ws2_32.lib")

struct ClientData
{
    SOCKET socket; // 클라이언트 소켓
    std::string nickname; // 클라이언트 닉네임
    bool isLobby = true; // 클라이언트가 로비에 있는지 아닌지 확인(접속하면 로비로 가기 때문에 True)
};

// JSON 라이브러리 (json.hpp가 같은 폴더에 있다고 가정)
#include "json.hpp"
using json = nlohmann::json;

#define MAX_BUFFER_SIZE (32768)
class TcpServer {
public:
    TcpServer(int port);
    ~TcpServer();

    void Start(); // 서버 시작 (리스닝)
    void SendToClient(int clientId, const json& data); // 특정 클라이언트에게 전송

private:
    void AcceptLoop(); // 클라이언트 접속 대기 루프
    void HandleClient(SOCKET clientSocket, int clientId); // 개별 클라이언트 통신 담당
    void RemoveClient(int clientId); // 퇴장 처리
    std::string AnsiToUtf8(const std::string& ansiStr); // 콘솔입력(CP949)을 UTF-8로 변환

    SOCKET serverSocket;
    int port;
    bool isRunning;
    json access_return_data;
    std::string nickname;

    // 클라이언트 관리를 위한 맵 (ID -> 소켓)
    std::map<int, ClientData> clients;

    // 스레드 충돌 방지를 위한 뮤텍스
    std::mutex clientMutex;

    // 클라이언트 ID 부여용 카운터
    int nextClientId = 1;
}; 