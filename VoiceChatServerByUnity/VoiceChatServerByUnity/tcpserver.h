#pragma once
#include <winsock2.h>
#include <ws2tcpip.h>
#include <map>
#include <thread>
#include <mutex>
#include <string>
#include <vector>

#pragma comment(lib, "ws2_32.lib")

// JSON 라이브러리 (json.hpp가 같은 폴더에 있다고 가정)
#include "json.hpp"
using json = nlohmann::json;

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

    SOCKET serverSocket;
    int port;
    bool isRunning;

    // 클라이언트 관리를 위한 맵 (ID -> 소켓)
    std::map<int, SOCKET> clients;

    // 스레드 충돌 방지를 위한 뮤텍스
    std::mutex clientMutex;

    // 클라이언트 ID 부여용 카운터
    int nextClientId = 1;
}; 