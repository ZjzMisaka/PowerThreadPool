# PowerThreadPool
<img src="https://www.nuget.org/Content/gallery/img/logo-header.svg?sanitize=true" height="30px">

![Nuget](https://img.shields.io/nuget/v/PowerThreadPool) ![Nuget](https://img.shields.io/nuget/dt/PowerThreadPool)  
Enables efficient thread pool management with callback implementation, granular control, customizable concurrency, and support for diverse task submissions.

## Download
PowerThreadPool is available as [Nuget Package](https://www.nuget.org/packages/PowerThreadPool/) now.

## Getting started
```csharp
PowerPool powerPool = new PowerPool(new ThreadPoolOption() { /* Some options */ });
powerPool.QueueWorkItem(() => 
{
    // DO SOMETHING
});
```
### With callback
```csharp
PowerPool powerPool = new PowerPool(new ThreadPoolOption() { /* Some options */ });
powerPool.QueueWorkItem(() => 
{
    // DO SOMETHING
    return result;
}, (res) => 
{
    // this callback of thread
    // running result: res.Result
});
```
### With option
```csharp
PowerPool powerPool = new PowerPool(new ThreadPoolOption() { /* Some options */ });
powerPool.QueueWorkItem(() => 
{
    // DO SOMETHING
    return result;
}, new ThreadOption()
{
    // Some options
});
```
### **API Summary**
### PowerPool
|name|summary|result|
|---|---|---|
|QueueWorkItem<...>(...)|Queues a method for execution. The method executes when a thread pool thread becomes available.|work id|
|Wait()|Blocks the calling thread until all of the threads terminates.|-|
|Wait(string id)|Blocks the calling thread until the thread terminates.|Return false if the thread isn't running|
|WaitAsync()|Blocks the calling thread until all of the threads terminates.|Task|
|WaitAsync(string id)|Blocks the calling thread until the thread terminates.|Return false if the thread isn't running|
|Stop(bool forceStop = false)|Stop all threads. If forceStop is true, Thread.Interrupt() and Thread.Join() will be called.|Return false if no thread running|
|StopAsync(bool forceStop = false)|Stop all threads. If forceStop is true, Thread.Interrupt() and Thread.Join() will be called.|(Task) Return false if no thread running|
|Stop(string id, bool forceStop = false)|Stop thread by id. If forceStop is true, Thread.Interrupt() and Thread.Join() will be called.|Return false if the thread isn't running|
|StopAsync(string id, bool forceStop = false)|Stop thread by id. If forceStop is true, Thread.Interrupt() and Thread.Join() will be called.|(Task) Return false if the thread isn't running|
|PauseIfRequested()|Call this function inside the thread logic where you want to pause when user call Pause(...)|-|
|StopIfRequested()|Call this function inside the thread logic where you want to stop when user call Stop(...)|-|
|CheckIfRequestedStop()|Call this function inside the thread logic where you want to check if requested stop (if user call Stop(...))|-|
|Pause()|Pause all threads|-|
|Resume(bool resumeThreadPausedById = false)|Resume all threads|-|
|Pause(string id)|Pause thread by id|If the work id exists|
|Resume(string id)|Resume thread by id|If the work id exists|
|Cancel()|Cancel all tasks that have not started running|-|
|Cancel(string id)|Cancel the task by id if the task has not started running|is succeed|
### **API List**
### PowerPool
#### Properties
```csharp
bool ThreadPoolRunning; // Get
bool ThreadPoolStopping; // Get
int IdleThreadCount; // Get
ThreadPoolOption ThreadPoolOption; // Get, Set
int WaitingWorkCount; // Get
IEnumerable<string> WaitingWorkerList; // Get
int RunningWorkerCount; // Get
IEnumerable<string> RunningWorkList; // Get
```
#### Events
```csharp
event ThreadPoolStartEventHandler ThreadPoolStart;
event ThreadPoolIdleEventHandler ThreadPoolIdle;
event ThreadStartEventHandler ThreadStart;
event ThreadEndEventHandler ThreadEnd;
event ThreadPoolTimeoutEventHandler ThreadPoolTimeout;
event ThreadTimeoutEventHandler ThreadTimeout;
event ThreadForceStopEventHandler ThreadForceStop;
```
#### Methods
```csharp
string QueueWorkItem(Action action, Action<ExcuteResult<object>> callBack = null);
string QueueWorkItem(Action action, ThreadOption option);
string QueueWorkItem(Action<object[]> action, object[] param, Action<ExcuteResult<object>> callBack = null);
string QueueWorkItem(Action<object[]> action, object[] param, ThreadOption option);
string QueueWorkItem<T1>(Action<T1> action, T1 param1, Action<ExcuteResult<object>> callBack = null);
string QueueWorkItem<T1>(Action<T1> action, T1 param1, ThreadOption option);
string QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, Action<ExcuteResult<object>> callBack = null);
string QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, ThreadOption option);
string QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3, Action<ExcuteResult<object>> callBack = null);
string QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3, ThreadOption option);
string QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExcuteResult<object>> callBack = null);
string QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4, ThreadOption option);
string QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExcuteResult<object>> callBack = null);
string QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, ThreadOption option);
string QueueWorkItem<T1, TResult>(Func<T1, TResult> function, T1 param1, Action<ExcuteResult<TResult>> callBack = null);
string QueueWorkItem<T1, TResult>(Func<T1, TResult> function, T1 param1, ThreadOption option);
string QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2, Action<ExcuteResult<TResult>> callBack = null);
string QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2, ThreadOption option);
string QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3, Action<ExcuteResult<TResult>> callBack = null);
string QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3, ThreadOption option);
string QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExcuteResult<TResult>> callBack = null);
string QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, ThreadOption option);
string QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExcuteResult<TResult>> callBack = null);
string QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, ThreadOption option);
string QueueWorkItem<TResult>(Func<TResult> function, Action<ExcuteResult<TResult>> callBack = null);
string QueueWorkItem<TResult>(Func<TResult> function, ThreadOption option);
string QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, Action<ExcuteResult<TResult>> callBack = null);
string QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, ThreadOption option);
void Wait();
bool Wait(string id);
async Task WaitAsync();
async Task<bool> WaitAsync(string id);
bool Stop(bool forceStop = false);
async Task<bool> StopAsync(bool forceStop = false);
bool Stop(string id, bool forceStop = false);
async Task<bool> StopAsync(string id, bool forceStop = false);
void PauseIfRequested();
void StopIfRequested();
bool CheckIfRequestedStop();
void Pause();
void Resume(bool resumeThreadPausedById = false);
bool Pause(string id);
bool Resume(string id);
void Cancel();
bool Cancel(string id);
```
### ExcuteResult\<TResult>
#### Properties
```csharp
// Work id.
string ID; // Get
// Result of the work.
TResult Result; // Get
// Succeed or failed.
Status Status; // Get
// If failed, Exception will be setted here.
Exception Exception; // Get
```
### Status
```csharp
enum Status { Succeed, Failed }
```
### ThreadPoolOption
#### Properties
```csharp
// The maximum number of threads that the thread pool can support.
int MaxThreads; // Get, Set
// The option for destroying threads in the thread pool.
DestroyThreadOption DestroyThreadOption; // Get, Set
// The total maximum amount of time that all threads in the thread pool are permitted to run collectively before they are terminated.
TimeoutOption Timeout; // Get, Set
// The default maximum amount of time a thread in the pool is allowed to run before it is terminated.
TimeoutOption DefaultThreadTimeout; // Get, Set
// The default callback function that is called when a thread finishes execution.
Action<ExecuteResult<object>> DefaultCallback; // Get, Set
```
### DestroyThreadOption
#### Properties
```csharp
// The amount of time a thread is kept alive after it finishes execution. If a new task is received within this time, the thread is reused; otherwise, it is destroyed.
int KeepAliveTime; // Get, Set
// The minimum number of threads that the thread pool should maintain at all times.
int MinThreads; // Get, Set
```
### ThreadOption
#### Properties
```csharp
// The custom work ID. If set to null, the thread pool will use a Guid as the work ID.
string CustomWorkID; // Get, Set
// The maximum amount of time the thread is allowed to run before it is terminated.
TimeoutOption Timeout; // Get, Set
// The callback function that is called when the thread finishes execution.
Action<ExecuteResult<TResult>> Callback; // Get, Set
// The priority level of the thread. Higher priority threads are executed before lower priority threads.
int Priority; // Get, Set
// A set of threads that this thread depends on. This thread will not start until all dependent threads have completed execution.
HashSet<string> Dependents; // Get, Set
```
### TimeoutOption
#### Properties
```csharp
// The maximum amount of time (ms)
int Duration;
// If forceStop is true, Thread.Interrupt() and Thread.Join() will be called.
bool ForceStop;
```
