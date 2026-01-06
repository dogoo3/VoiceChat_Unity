#include "TcpServer.h"
#include <iostream>
#include <fstream>
#include <set>

TcpServer::TcpServer(int port) : port(port), serverSocket(INVALID_SOCKET), isRunning(false) {
    WSADATA wsaData;

    // 클라이언트가 서버에 접속했을 때 
    access_return_data["type"] = "login_result";
    access_return_data["content"] = true;
    access_return_data["timestamp"] = 0;

    WSAStartup(MAKEWORD(2, 2), &wsaData);
}

TcpServer::~TcpServer() {
    isRunning = false;
    closesocket(serverSocket);
    WSACleanup();
}

void TcpServer::Start() {
    serverSocket = socket(AF_INET, SOCK_STREAM, 0);
    sockaddr_in serverAddr;
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = INADDR_ANY;
    serverAddr.sin_port = htons(port);

    if (bind(serverSocket, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
        std::cerr << "Binding failed!" << std::endl;
        return;
    }

    if (listen(serverSocket, SOMAXCONN) == SOCKET_ERROR) {
        std::cerr << "Listening failed!" << std::endl;
        return;
    }

    isRunning = true;
    std::cout << "=== 서버 시작 (Port: " << port << ") ===" << std::endl;

    // 접속 대기를 별도 스레드에서 실행
    std::thread(&TcpServer::AcceptLoop, this).detach();
}

void TcpServer::AcceptLoop() {
    while (isRunning) {
        sockaddr_in clientAddr;
        int clientSize = sizeof(clientAddr);
        SOCKET clientSocket = accept(serverSocket, (sockaddr*)&clientAddr, &clientSize);

        if (clientSocket != INVALID_SOCKET) {
            std::lock_guard<std::mutex> lock(clientMutex);
            int id = nextClientId++;
            clients[id].socket = clientSocket;

            // 입장 로그
            std::cout << "[입장] Client ID: " << id << " 연결됨." << std::endl;

            // 개별 클라이언트를 담당할 스레드 생성
            std::thread(&TcpServer::HandleClient, this, clientSocket, id).detach();
            
            //if (clients.count(id)) {
            //    std::string msg = access_return_data.dump(); // JSON을 문자열로 변환
            //    send(clients[id].socket, msg.c_str(), msg.length(), 0);
            //    std::cout << "[접속 완료 후 전송 to " << id << "] " << msg << std::endl;
            //}
            //else {
            //    std::cout << "[접속 완료 후 전송 실패] 존재하지 않는 Client ID: " << id << std::endl;
            //}
        }
    }
}

void TcpServer::HandleClient(SOCKET clientSocket, int clientId) {
    char buffer[4096];

    while (isRunning) {
        ZeroMemory(buffer, 4096);
        int bytesReceived = recv(clientSocket, buffer, 4096, 0);

        if (bytesReceived <= 0) {
            RemoveClient(clientId);
            break;
        }

        std::string rawData(buffer, bytesReceived);

        try {
            // JSON 파싱
            json receivedJson = json::parse(rawData);
            if (receivedJson["common"]["type"] == "login") // 로그인 프로토콜이 들어왔을 때
            {
                std::string nickname = receivedJson["common"]["content"];
                bool isDuplicate = false;

                for (auto iter = clients.begin(); iter != clients.end(); iter++) // 닉네임 중복 검사
                {
                    if (iter->second.nickname == nickname) // 접속 중인 클라이언트들의 닉네임과 같을 경우
                    {
                        isDuplicate = true;
                        break;
                    }
                }
                if (isDuplicate) // 닉네임이 중복되면 로비 이동을 허용하지 않는 플래그를 전송한다
                {
                    std::cout << "닉네임중복" << std::endl;
                    access_return_data["type"] = "nickname_check_result";
                    access_return_data["content"] = false;
                    access_return_data["timestamp"] = time(0);
                    SendToClient(clientId, access_return_data);
                    RemoveClient(clientId); // 재인증을 거쳐야 하므로 서버에서 해당 클라이언트 연결 해제
                }
                else
                {
                    std::cout << "정상닉네임" << std::endl;
                    access_return_data["type"] = "nickname_check_result";
                    access_return_data["content"] = true;
                    access_return_data["timestamp"] = time(0);
                    clients[clientId].nickname = nickname; // 닉네임 저장
                    SendToClient(clientId, access_return_data);
                }
            }

            // 로그 출력
            //std::cout << "[수신 from " << clientId << "] " << receivedJson.dump() << std::endl;
            // 
            // (옵션) 파일 저장
            std::ofstream logFile("server_log.txt", std::ios::app);
            logFile << time(0) << "[Client " << clientId << "] " << receivedJson.dump() << std::endl;
            logFile.close();

        }
        catch (json::parse_error& e) {
            std::cerr << "[JSON 에러] " << e.what() << std::endl;
        }
    }
}

void TcpServer::RemoveClient(int clientId) {
    std::lock_guard<std::mutex> lock(clientMutex);
    if (clients.count(clientId)) {
        closesocket(clients[clientId].socket);
        clients.erase(clientId);
        std::cout << "[퇴장] Client ID: " << clientId << " 연결 해제." << std::endl;
    }
}

void TcpServer::SendToClient(int clientId, const json& data) {
    std::lock_guard<std::mutex> lock(clientMutex);
    if (clients.count(clientId)) {
        std::string msg = data.dump(); // JSON을 문자열로 변환
        send(clients[clientId].socket, msg.c_str(), msg.length(), 0);
        std::cout << "[전송 to " << clientId << "] " << msg << std::endl;
    }
    else {
        std::cout << "[전송 실패] 존재하지 않는 Client ID: " << clientId << std::endl;
    }
}