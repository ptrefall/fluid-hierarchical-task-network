#pragma once

namespace FluidHTN
{
enum class ConsoleColor
{
    Red = FOREGROUND_INTENSITY | FOREGROUND_RED,
    DarkRed = FOREGROUND_RED,
    Blue = FOREGROUND_INTENSITY | FOREGROUND_BLUE,
    DarkBlue = FOREGROUND_BLUE,
    Green = FOREGROUND_INTENSITY | FOREGROUND_GREEN,
    DarkGreen = FOREGROUND_GREEN,
    Black = 0,
    White = FOREGROUND_INTENSITY | FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE,
    Yellow = FOREGROUND_INTENSITY | FOREGROUND_RED | FOREGROUND_GREEN,
    DarkYellow =  FOREGROUND_RED | FOREGROUND_GREEN,
};
class Debug
{
public:
    static std::string DepthToString(int depth)
    {
        std::string s = ""s;
        for (auto i = 0; i < depth; i++)
        {
            s += "\t"s;
        }

        s += "- "s;
        return s;
    }
};
struct IBaseDecompositionLogEntry
{
    std::string  Name;
    std::string  Description;
    int          Depth;
    ConsoleColor Color;
};

template <typename T>
struct IDecompositionLogEntry : public IBaseDecompositionLogEntry
{
public:
    std::shared_ptr<T> _Entry;
};
} // namespace FluidHTN
