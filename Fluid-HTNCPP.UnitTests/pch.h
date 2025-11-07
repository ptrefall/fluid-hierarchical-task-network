// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H

// add headers that you want to pre-compile here
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#define VC_EXTRALEAN
#include <windows.h>

#include <string>
#include <unordered_map>
#include <unordered_set>
#include <memory>
#include <queue>
#include <stack>
#include <vector>
#include <stdexcept>
#include <functional>
#include <cstdlib>
#include <ctime>

#include "CoreIncludes/STLTypes.h"

#ifndef FHTN_FATAL_EXCEPTION
#define FHTN_FATAL_EXCEPTION(condition, msg)                                                                                          \
    if (!(condition))                                                                                                              \
    {                                                                                                                              \
        throw std::exception(msg);                                                                                                \
    }

#endif

#ifndef FHTN_FATAL_EXCEPTION_V
#define FHTN_FATAL_EXCEPTION_V(condition, fmt, ...)  this is for UE4 checkf, verifymsg etc. do not t use elsewhere
#endif

using namespace std::string_literals;

#endif //PCH_H
