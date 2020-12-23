// 
// Define aliases and wrappers for STL types here so that custom implementations can override them (UE4 for example).
//
#pragma once

#if !FHTN_USING_CUSTOM_STL

template<typename T>
using SharedPtr = std::shared_ptr<T>;

template<typename T, class... ARGS>
SharedPtr<T> MakeSharedPtr(ARGS&&... args)
{
    return std::make_shared<T>(std::forward<ARGS>(args)...);
}
template <typename T>
using EnableSharedFromThis = std::enable_shared_from_this<T>;

#define SharedFromThis()  shared_from_this()

template <typename T,typename U>
SharedPtr<T> StaticCastPtr(const SharedPtr<U>& Other)
{
    return std::static_pointer_cast<T>(Other);
}

template<typename T>
class ArrayType 
{
private:
    std::vector<T> vec;
public:
    ArrayType(){}
    ArrayType(size_t s) : vec(s){}
    void Add(const T& x) {vec.push_back(x);}
    size_t size() const {return vec.size();}
    void clear() {return vec.clear();}
    void PopBack(){vec.pop_back();}
    void resize(size_t n) {vec.resize(n);}

    T& Back() {return vec.back();}

    T* begin() {return vec.begin();}
    auto end() {return vec.end();}

    T& operator[](size_t index){return vec[index];}
    const T& operator[](size_t index)const {return vec[index];}
};

template<typename T>
class Queue
{
private:
    std::queue<T> q;

public:
    void   push(const T& x) { q.push(x); }
    void   push(T&& x) { q.push(std::move(x)); }
    void   pop() { q.pop(); }
    T&     front() { return q.front(); }
    size_t size() { return q.size(); }
    bool   empty() { return q.empty(); }
    void   clear() { q = std::queue<T>(); }
};
template<typename T>
class Stack
{
private:
    std::stack<T> s;

public:
    void   push(const T& x) { s.push(x); }
    void   push(T&& x) { s.push(std::move(x)); }
    void   pop() { return s.pop(); }
    T&     top() { return s.top(); }
    size_t size() { return s.size(); }
    bool   empty() { return s.empty(); }
    void   clear() { s = std::stack<T>(); }
};

template<typename T,typename U>
class Map
{
private:
    std::unordered_map<T, U> m;
public:
    template<typename V>
    auto Insert(V&& x) -> decltype(m.insert(std::forward<V>(x))) { return m.insert(std::forward<V>(x)); }

    auto Find(T x) -> decltype(m.find(std::forward<T>(x))) { return m.find(std::forward<T>(x)); }

    auto End() { return m.end(); }
};
template<typename T>
class Set
{
private:
    std::unordered_set<T> s;
public:
    template<typename V>
    auto Insert(V&& x) -> decltype(s.insert(std::forward<V>(x))) { return s.insert(std::forward<V>(x)); }

    auto Find(T x) -> decltype(s.find(std::forward<T>(x))) { return s.find(std::forward<T>(x)); }

    auto Contains(T x) { return (s.find(x) !=  s.end()); }
};

template<typename T, typename U>
auto MakePair(T&& A, U&& B) -> decltype(std::make_pair(std::forward<T>(A), std::forward<U>(B)) )
{
    return std::make_pair(std::forward<T>(A), std::forward<U>(B));
}

template<typename P1, typename P2>
class Pair
{
private:
    std::pair<P1,P2> p;

public:
    Pair(P1 X, P2 Y): p(X,Y){}
    P1& First() { return p.first; }
    P2& Second() { return p.second; }

};

using StringType = std::string;

template<typename T>
StringType ToString(const T& arg)
{
    return std::to_string(arg);
}

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

#else
#include "STLReplacementTypes.h"
#endif !FHTN_USING_CUSTOM_STL
