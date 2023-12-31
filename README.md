# PowerThreadPool
![Logo](icon.png "Logo")

![Nuget](https://img.shields.io/nuget/v/PowerThreadPool?style=for-the-badge)
![Nuget](https://img.shields.io/nuget/dt/PowerThreadPool?style=for-the-badge)
![GitHub release (with filter)](https://img.shields.io/github/v/release/ZjzMisaka/PowerThreadPool?style=for-the-badge)
![GitHub Repo stars](https://img.shields.io/github/stars/ZjzMisaka/PowerThreadPool?style=for-the-badge)
![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/ZjzMisaka/PowerThreadPool/test.yml?style=for-the-badge)
![Codecov](https://img.shields.io/codecov/c/github/ZjzMisaka/PowerThreadPool?style=for-the-badge)

Enables efficient thread pool management with callback implementation, granular control, customizable concurrency, and support for diverse task submissions.  

## Documentation
Read the [Wiki](https://github.com/ZjzMisaka/PowerThreadPool/wiki) here.  

## Download
PowerThreadPool is available as [Nuget Package](https://www.nuget.org/packages/PowerThreadPool/) now.  
Support: Net46+ | Net5.0+  

## Features
1. [Work Control](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control)
    - [Stop](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Pause](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Resume](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Force Stop](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#force-stop)
    - [Wait](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#wait)
    - [Cancel](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#cancel)
2. [Work Dependency](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Dependency)
3. [Thread Pool Sizing](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Thread-Pool-Sizing)
4. [Idle Thread Timeout](https://github.com/ZjzMisaka/PowerThreadPool/wiki/DestroyThreadOption)
5. [Work Priority | Thread Priority](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Priority)
6. [Work Timeout | Cumulative Work Timeout](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Timeout)
7. [Callback](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Callback)
8. [Error Handling](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Callback)
9. [Runtime Status](https://github.com/ZjzMisaka/PowerThreadPool/wiki/PowerPool#properties)
10. [Load Balancing](https://en.wikipedia.org/wiki/Work_stealing)

## Getting started
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
## APIs
### [API Summary](https://github.com/ZjzMisaka/PowerThreadPool/wiki/API-Summary)  
- #### [PowerPool](https://github.com/ZjzMisaka/PowerThreadPool/wiki/PowerPool)  
- #### [ExcuteResult](https://github.com/ZjzMisaka/PowerThreadPool/wiki/ExcuteResult)  
- #### [PowerPoolOption](https://github.com/ZjzMisaka/PowerThreadPool/wiki/PowerPoolOption)  
- #### [DestroyThreadOption](https://github.com/ZjzMisaka/PowerThreadPool/wiki/DestroyThreadOption)  
- #### [WorkOption](https://github.com/ZjzMisaka/PowerThreadPool/wiki/WorkOption)  
- #### [TimeoutOption](https://github.com/ZjzMisaka/PowerThreadPool/wiki/TimeoutOption)  
