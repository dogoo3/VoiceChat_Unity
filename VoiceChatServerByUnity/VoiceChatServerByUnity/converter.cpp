#include "converter.h"

std::string AnsiToUtf8(const std::string& ansiStr)
{
    if (ansiStr.empty())
        return std::string();

    // 1. ANSI (CP949) -> Unicode (UTF-16)
    int sizeNeeded = MultiByteToWideChar(CP_ACP, 0, &ansiStr[0], (int)ansiStr.size(), NULL, 0);
    std::wstring wstrTo(sizeNeeded, 0);
    MultiByteToWideChar(CP_ACP, 0, &ansiStr[0], (int)ansiStr.size(), &wstrTo[0], sizeNeeded);

    // 2. Unicode (UTF-16) -> UTF-8
    sizeNeeded = WideCharToMultiByte(CP_UTF8, 0, &wstrTo[0], (int)wstrTo.size(), NULL, 0, NULL, NULL);
    std::string strTo(sizeNeeded, 0);
    WideCharToMultiByte(CP_UTF8, 0, &wstrTo[0], (int)wstrTo.size(), &strTo[0], sizeNeeded, NULL, NULL);

    return strTo;
}