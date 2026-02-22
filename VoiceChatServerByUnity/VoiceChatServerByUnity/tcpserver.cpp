#include "TcpServer.h"
#include <iostream>
#include <fstream>
#include <set>

TcpServer::TcpServer(int port) : port(port), serverSocket(INVALID_SOCKET), isRunning(false) {
    WSADATA wsaData;

    // 임의 초기화
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
    char buffer[MAX_BUFFER_SIZE];

    while (isRunning) {
        ZeroMemory(buffer, MAX_BUFFER_SIZE);
        int bytesReceived = recv(clientSocket, buffer, MAX_BUFFER_SIZE, 0);

        if (bytesReceived <= 0) {
            RemoveClient(clientId);
            break;
        }

        std::string rawData(buffer, bytesReceived);

        try {
            // JSON 파싱
            json receivedJson = json::parse(rawData);
            const std::string type_name = receivedJson["common"]["type"];
            std::cout << clientId << "에게 " << type_name << "프로토콜 들어옴!" << std::endl;
            if (type_name == "login") // 로그인 프로토콜이 들어왔을 때
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
                    std::cout << clients.size() << std::endl;
                    std::cout << "정상닉네임" << std::endl;
                    access_return_data["type"] = "nickname_check_result";
                    access_return_data["content"] = true;
                    access_return_data["timestamp"] = time(0);
                    access_return_data["mynickname"] = nickname;

                    clients[clientId].nickname = nickname; // 닉네임 저장
                    std::cout << clients.size() << std::endl;

                    std::vector<std::string> users_name;
                    for (auto iter = clients.begin(); iter != clients.end(); iter++) // 닉네임들을 저장
                    {
                        users_name.push_back(iter->second.nickname);
                    }
                    access_return_data["users_name"] = users_name;
                    SendToClient(clientId, access_return_data);

                    // 이미 방에 접속해 있는 유저들에게도 새로운 유저가 들어왔다는 것을 알려줘야 함
                    for (auto iter = clients.begin(); iter != clients.end(); iter++) // 닉네임 중복 검사
                    {
                        if (iter->second.nickname != nickname) // 다른 클라이언트들에게 전송. 내 닉네임일 경우 전송하지 않음.
                        {
                            json temp;
                            std::cout << nickname + "체크" << std::endl;
                            temp["type"] = "add_user";
                            temp["add_username"] = nickname;
                            SendToClient(iter->first, temp);
                        }
                    }
                }
            }
            else if (type_name == "arraytest")
            {
                std::cout << receivedJson["common"]["content"] << std::endl;
                std::cout << sizeof(receivedJson["common"]["content"]) << std::endl;
                std::cout << sizeof(receivedJson["common"]["content"][0]) << std::endl;
                std::cout << receivedJson["common"]["content"].size() << std::endl;

                json tempjson;
                float asd[3] = { receivedJson["common"]["content"][0], receivedJson["common"]["content"][12], receivedJson["common"]["content"][1763] };
                tempjson["type"] = "array_test_result";
                tempjson["content"] = asd;

                SendToClient(clientId, tempjson);
            }
            else if (type_name == "userexit") // 특정한 유저가 나갔으면
            {
                // 다른 유저들에게 그 플레이어가 나갔다는 정보를 보낸다
                json tempjson;
                tempjson["type"] = "anotherexit";
                tempjson["exit_nickname"] = clients[clientId].nickname;

                for (auto iter = clients.begin(); iter != clients.end(); iter++)
                {
                    if (iter->first != clientId) // 나간 사람의 클라이언트 ID가 아닌 사람들에게 보낸다
                    {
                        SendToClient(iter->first, tempjson);
                    }
                }

                // 그 유저에게는 나가도 된다는 packet을 보낸다
                tempjson["type"] = "myexit";
                SendToClient(clientId, tempjson);
                RemoveClient(clientId);
            }
            else if (type_name == "soundframe")
            {
                std::cout << "----------------------------" << std::endl;
                std::cout << sizeof(receivedJson["common"]["content"]) << std::endl;
                std::cout << receivedJson["common"]["content"].size() << std::endl;
                // 보낼 데이터를 정하고, 음성 데이터를 바이너리화한다
                std::string typeStr = "anotheraudio"; // 또는 receivedJson["common"]["type"];
                int channel = receivedJson["channel"]; // 정수라고 가정
                std::vector<float> audioData = receivedJson["common"]["content"].get<std::vector<float>>();

                // 바이너리 버퍼 생성에 필요한 크기를 계산한다
                int typeLen = typeStr.length();
                int floatCount = audioData.size();

                // Payload 크기 = (Type길이 변수) + (Type문자열) + (Channel 변수) + (Float개수 변수) + (Float배열)
                int payloadSize = sizeof(int) + typeLen + sizeof(int) + sizeof(int) + (floatCount * sizeof(float));
                int totalSize = sizeof(int) + payloadSize; // 맨 앞 Payload 크기(4바이트) 포함
                
                // 데이터를 담을 버퍼 할당 및 복사
                std::vector<char> sendBuffer(totalSize);
                int offset = 0;

                // (1) Payload 전체 크기
                memcpy(sendBuffer.data() + offset, &payloadSize, sizeof(int)); offset += sizeof(int);

                // (2) Type 문자열 길이
                memcpy(sendBuffer.data() + offset, &typeLen, sizeof(int)); offset += sizeof(int);

                // (3) Type 문자열 데이터
                memcpy(sendBuffer.data() + offset, typeStr.c_str(), typeLen); offset += typeLen;

                // (4) Channel 번호
                memcpy(sendBuffer.data() + offset, &channel, sizeof(int)); offset += sizeof(int);

                // (5) Float 데이터 개수
                memcpy(sendBuffer.data() + offset, &floatCount, sizeof(int)); offset += sizeof(int);

                // (6) Float 배열 데이터
                memcpy(sendBuffer.data() + offset, audioData.data(), floatCount * sizeof(float)); offset += (floatCount * sizeof(float));

                // 받은 사람의 client를 제외한 모든 client에게 보내준다
                //json tempjson;
                //tempjson["type"] = "anotheraudio";
                //tempjson["channel"] = receivedJson["channel"];
                //std::cout << receivedJson["channel"] << std::endl;

                for (auto iter = clients.begin(); iter != clients.end(); iter++)
                {
                    if (iter->first != clientId)
                    {
                        send(iter->second.socket, sendBuffer.data(), totalSize, 0);
                        //SendToClient(iter->first, tempjson);
                    }
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

//void TcpServer::SendToClient(int clientId, const json& data) {
//    std::lock_guard<std::mutex> lock(clientMutex);
//    if (clients.count(clientId)) {
//        std::string msg = data.dump(); // JSON을 문자열로 변환
//        send(clients[clientId].socket, msg.c_str(), msg.length(), 0);
//        std::cout << "[전송 to " << clientId << "] " << msg << std::endl;
//    }
//    else {
//        std::cout << "[전송 실패] 존재하지 않는 Client ID: " << clientId << std::endl;
//    }
//}

void TcpServer::SendToClient(int clientId, const json& data) {
    std::lock_guard<std::mutex> lock(clientMutex);

    if (clients.count(clientId)) {
        std::string msg = data.dump(); // JSON을 문자열로 변환
        int payloadSize = msg.length(); // 실제 JSON 데이터의 길이

        // 1. 전송할 전체 버퍼 생성: 4바이트(크기 정보) + 실제 데이터 길이
        std::vector<char> sendBuffer(sizeof(int) + payloadSize);

        // 2. 맨 앞 4바이트에 payloadSize 값을 복사
        memcpy(sendBuffer.data(), &payloadSize, sizeof(int));

        // 3. 그 뒤(4바이트 이후)에 실제 JSON 문자열 데이터를 복사
        memcpy(sendBuffer.data() + sizeof(int), msg.c_str(), payloadSize);

        // 4. 합쳐진 바이너리 버퍼를 전송
        send(clients[clientId].socket, sendBuffer.data(), sendBuffer.size(), 0);

        // 로그 출력 (디버깅용)
        std::cout << "[전송 to " << clientId << "] " << msg << " (Payload: " << payloadSize << " bytes)" << std::endl;
    }
    else {
        std::cout << "[전송 실패] 존재하지 않는 Client ID: " << clientId << std::endl;
    }
}