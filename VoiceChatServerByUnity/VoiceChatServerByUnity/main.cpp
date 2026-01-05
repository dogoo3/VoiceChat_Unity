#include "TcpServer.h"
#include <iostream>

int main() {
    TcpServer server(8080);
    server.Start();

    // 메인 스레드는 관리자 입력을 처리함
    while (true) {
        int targetId;
        std::string messageContent;

        std::cout << "\n[관리자 명령] 보낼 Client ID와 메시지를 입력하세요 (예: 1 Hello): ";
        if (!(std::cin >> targetId >> messageContent)) break;

        // 보낼 JSON 데이터 생성
        json msg;
        msg["type"] = "admin_message";
        msg["content"] = messageContent;
        msg["timestamp"] = time(0);

        server.SendToClient(targetId, msg);
    }

    return 0;
}