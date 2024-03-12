# PowerThreadPool
![icon](https://raw.githubusercontent.com/ZjzMisaka/PowerThreadPool/main/icon.png)

![Nuget](https://img.shields.io/nuget/v/PowerThreadPool?style=for-the-badge)
![Nuget](https://img.shields.io/nuget/dt/PowerThreadPool?style=for-the-badge)
![GitHub release (with filter)](https://img.shields.io/github/v/release/ZjzMisaka/PowerThreadPool?style=for-the-badge)
![GitHub Repo stars](https://img.shields.io/github/stars/ZjzMisaka/PowerThreadPool?style=for-the-badge)
![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/ZjzMisaka/PowerThreadPool/test.yml?style=for-the-badge)
![Codecov](https://img.shields.io/codecov/c/github/ZjzMisaka/PowerThreadPool?style=for-the-badge)

Offers an comprehensive and efficient thread pool with granular work control, flexible concurrency, and robust error handling, alongside an easy-to-use API for diverse task submissions.  

## Documentation
Read the [Wiki](https://github.com/ZjzMisaka/PowerThreadPool/wiki) here.  

## Download
PowerThreadPool is available as [Nuget Package](https://www.nuget.org/packages/PowerThreadPool/) now.  
Support: Net46+ | Net5.0+  

## Features
- [Pool Control | Work Control](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control)
    - [Stop](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Pause](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Resume](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Force Stop](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#force-stop)
    - [Wait](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#wait)
    - [Cancel](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#cancel)
- [Work Dependency](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Dependency)
- [Thread Pool Sizing](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Thread-Pool-Sizing)
    - [Idle Thread Scheduled Destruction](https://github.com/ZjzMisaka/PowerThreadPool/wiki/DestroyThreadOption)
    - [Thread Starvation Countermeasures (Long-running Task Support)](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Thread-Pool-Sizing#thread-starvation)
- [Work Priority | Thread Priority](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Priority)
- [Work Timeout | Cumulative Work Timeout](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Timeout)
- [Work Callback | Default Callback](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Callback)
- [Error Handling](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Callback)
- [Runtime Status](https://github.com/ZjzMisaka/PowerThreadPool/wiki/PowerPool#properties)
- [Load Balancing](https://en.wikipedia.org/wiki/Work_stealing)
- [Lock-Free](https://en.wikipedia.org/wiki/Non-blocking_algorithm)

## Getting started
### Simple example: run a work
```csharp
PowerPool powerPool = new PowerPool(new PowerPoolOption() { /* Some options */ });
powerPool.QueueWorkItem(() => 
{
    // DO SOMETHING
});
```

### With callback
```csharp
PowerPool powerPool = new PowerPool(new PowerPoolOption() { /* Some options */ });
powerPool.QueueWorkItem(() => 
{
    // DO SOMETHING
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
    // DO SOMETHING
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
- Asterisk (*) denotes an optional parameter, either a WorkOption or a delegate (Action\<ExecuteResult\<object\>\> or Action\<ExecuteResult\<TResult\>\>), depending on whether the first parameter is an Action or a Func. 
- In places where you see ellipses (...), you can provide up to five generic type parameters. 

## APIs
### [API Summary](https://github.com/ZjzMisaka/PowerThreadPool/wiki/API-Summary)  
- #### [PowerPool](https://github.com/ZjzMisaka/PowerThreadPool/wiki/PowerPool)  
- #### [ExecuteResult](https://github.com/ZjzMisaka/PowerThreadPool/wiki/ExecuteResult)  
- #### [PowerPoolOption](https://github.com/ZjzMisaka/PowerThreadPool/wiki/PowerPoolOption)  
- #### [DestroyThreadOption](https://github.com/ZjzMisaka/PowerThreadPool/wiki/DestroyThreadOption)  
- #### [WorkOption](https://github.com/ZjzMisaka/PowerThreadPool/wiki/WorkOption)  
- #### [TimeoutOption](https://github.com/ZjzMisaka/PowerThreadPool/wiki/TimeoutOption)  
