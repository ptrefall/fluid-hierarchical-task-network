#pragma once
#include "CoreIncludes/STLTypes.h"

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
    static StringType DepthToString(int depth)
    {
        StringType s = ""s;
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
    StringType  Name;
    StringType  Description;
    int          Depth;
    ConsoleColor Color;
};

template <typename T>
struct IDecompositionLogEntry : public IBaseDecompositionLogEntry
{
public:
    SharedPtr<T> _Entry;
};
} // namespace FluidHTN
