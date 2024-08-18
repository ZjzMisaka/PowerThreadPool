# PowerThreadPool
![icon](https://raw.githubusercontent.com/ZjzMisaka/PowerThreadPool/main/icon.png)

![Nuget](https://img.shields.io/nuget/v/PowerThreadPool?style=for-the-badge)
![Nuget](https://img.shields.io/nuget/dt/PowerThreadPool?style=for-the-badge)
![GitHub release (with filter)](https://img.shields.io/github/v/release/ZjzMisaka/PowerThreadPool?style=for-the-badge)
![GitHub Repo stars](https://img.shields.io/github/stars/ZjzMisaka/PowerThreadPool?style=for-the-badge)
![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/ZjzMisaka/PowerThreadPool/test.yml?style=for-the-badge)
![Codecov](https://img.shields.io/codecov/c/github/ZjzMisaka/PowerThreadPool?style=for-the-badge)

A comprehensive and efficient lock-free thread pool with granular work control, flexible concurrency, and robust error handling, alongside an easy-to-use API for diverse work submissions.  

## Documentation
Read the [Wiki](https://github.com/ZjzMisaka/PowerThreadPool/wiki) here.  

## Installation
If you want to include PowerThreadPool in your project, you can [install it directly from NuGet](https://www.nuget.org/packages/PowerThreadPool/).  
Support: Net40+ | Net5.0+ | netstandard2.0+  

## Features
- [Pool Control | Work Control](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control)
    - [Stop](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Pause](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Resume](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Force Stop](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#force-stop)
    - [Wait](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#wait)
    - [Fetch](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#fetch)
    - [Cancel](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#cancel)
- [Thread Pool Sizing](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Thread-Pool-Sizing)
    - [Idle Thread Scheduled Destruction](https://github.com/ZjzMisaka/PowerThreadPool/wiki/DestroyThreadOption)
    - [Thread Starvation Countermeasures (Long-running Work Support)](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Thread-Pool-Sizing#thread-starvation)
- [Work Callback | Default Callback](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Callback)
- [Parallel Execution](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Parallel-Execution)
    - [For](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Parallel-Execution#For)
    - [ForEach](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Parallel-Execution#ForEach)
- [Work Priority | Thread Priority](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Priority)
- [Error Handling](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Error-Handling)
    - [Retry](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Retry)
- [Work Timeout | Cumulative Work Timeout](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Timeout)
- [Work Dependency](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Dependency)
- [Work Group](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Group)
    - [Group Control](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Group#group-control)
    - [Group Relation](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Group-Relation)
- [Events](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Events)
- [Runtime Status](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Runtime-Status)
- [Queue Type (FIFO | LIFO)](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Queue-Type)
- [Load Balancing](https://en.wikipedia.org/wiki/Work_stealing)
- [Lock-Free](https://en.wikipedia.org/wiki/Non-blocking_algorithm)

## Getting started
### Simple example: run a work
```csharp
PowerPool powerPool = new PowerPool();
powerPool.QueueWorkItem(() => 
{
    // Do something
});
```

### With callback
```csharp
PowerPool powerPool = new PowerPool(new PowerPoolOption() { /* Some options */ });
powerPool.QueueWorkItem(() => 
{
    // Do something
    return result;
}, (res) => 
{
    // Callback of the work
});
```

### With option
```csharp
PowerPool powerPool = new PowerPool(new PowerPoolOption() { /* Some options */ });
powerPool.QueueWorkItem(() => 
{
    // Do something
    return result;
}, new WorkOption()
{
    // Some options
});
```

### Reference
``` csharp
string QueueWorkItem<T1, ...>(Action<T1, ...> action, T1 param1, ..., *);
string QueueWorkItem(Action action, *);
string QueueWorkItem(Action<object[]> action, object[] param, *);
string QueueWorkItem<T1, ..., TResult>(Func<T1, ..., TResult> function, T1 param1, ..., *);
string QueueWorkItem<TResult>(Func<TResult> function, *);
string QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, *);
```
- Asterisk (*) denotes an optional parameter, either a WorkOption or a delegate (`Action<ExecuteResult<object>>` or `Action<ExecuteResult<TResult>>`), depending on whether the first parameter is an Action or a Func. 
- In places where you see ellipses (...), you can provide up to five generic type parameters.

## More
[Testing And Performance Analysis](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Testing-And-Performance-Analysis) | [Feature Comparison](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Feature-Comparison)
