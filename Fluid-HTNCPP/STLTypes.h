// 
// Define aliases and wrappers for STL types here so that custom implementations can override them (UE4 for example).
//
#pragma once

template<typename T>
using SharedPtr = std::shared_ptr<T>;

template<typename T, class... ARGS>
SharedPtr<T> MakeSharedPtr(ARGS&&... args)
{
    return std::make_shared<T>(std::forward<ARGS>(args)...);
}

template <typename T,typename U>
SharedPtr<T> StaticCastPtr(const SharedPtr<U>& Other)
{
    return std::static_pointer_cast<T>(Other);
}

template<typename T>
using ArrayType = std::vector<T>;

using StringType = std::string;

template<typename T>
StringType ToString(const T& arg)
{
    return std::to_string(arg);
}

template <typename T>
using EnableSharedFromThis = std::enable_shared_from_this<T>;

#define SharedFromThis()  shared_from_this()

