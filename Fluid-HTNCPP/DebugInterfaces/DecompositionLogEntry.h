#pragma once

namespace FluidHTN
{
enum class ConsoleColor
{
    Black,
    Red,
    DarkRed,
    Blue,
    DarkBlue,
    Green,
    DarkGreen,
    White,
    Yellow,
    DarkYellow 
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
