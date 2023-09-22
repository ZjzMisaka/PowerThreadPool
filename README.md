# PowerThreadPool
![Logo](icon.png "Logo")

![Nuget](https://img.shields.io/nuget/v/PowerThreadPool)
![Nuget](https://img.shields.io/nuget/dt/PowerThreadPool)
![GitHub release (with filter)](https://img.shields.io/github/v/release/ZjzMisaka/PowerThreadPool)
![GitHub Repo stars](https://img.shields.io/github/stars/ZjzMisaka/PowerThreadPool)
![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/ZjzMisaka/PowerThreadPool/test.yml)
![Codecov](https://img.shields.io/codecov/c/github/ZjzMisaka/PowerThreadPool)

Enables efficient thread pool management with callback implementation, granular control, customizable concurrency, and support for diverse task submissions.  

Read the [Wiki](https://github.com/ZjzMisaka/PowerThreadPool/wiki) here.  
## Download
PowerThreadPool is available as [Nuget Package](https://www.nuget.org/packages/PowerThreadPool/) now.

## Features
1. Work Control
    - Wait
    - Stop
    - Force Stop
    - Pause
    - Resume
    - Cancel
2. Work Dependency
3. Thread Pool Sizing
4. Thread Reuse
5. Idle Thread Timeout
6. Work Priority
7. Thread Priority
8. Work Timeout
9. Cumulative Work Timeout
10. Callback
11. Error Handling
12. Runtime Status

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
