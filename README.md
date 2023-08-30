# PowerThreadPool
<img src="https://www.nuget.org/Content/gallery/img/logo-header.svg?sanitize=true" height="30px">

Enables efficient ThreadPool management with callback implementation, granular control, customizable concurrency, and support for diverse task submissions.

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
### With Option
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
|QueueWorkItem<...>(...)|Queues a method for execution. The method executes when a thread pool thread becomes available.|thread id|
|Wait()|Blocks the calling thread until all of the threads terminates.|-|
|Wait(string id)|Blocks the calling thread until the thread terminates.|Return false if the thread isn't running|
|WaitAsync()|Blocks the calling thread until all of the threads terminates.|Task|
|WaitAsync(string id)|Blocks the calling thread until the thread terminates.|Return false if the thread isn't running|
|Stop(bool forceStop = false)|Stop all threads. If forceStop is true, Thread.Interrupt() and Thread.Join() will be called.|-|
|StopAsync(bool forceStop = false)|Stop all threads. If forceStop is true, Thread.Interrupt() and Thread.Join() will be called.|Task|
|Stop(string id, bool forceStop = false)|Stop thread by id. If forceStop is true, Thread.Interrupt() and Thread.Join() will be called.|If thread is in progress during the invocation|
|StopAsync(string id, bool forceStop = false)|Stop thread by id. If forceStop is true, Thread.Interrupt() and Thread.Join() will be called.|(Task) If thread is in progress during the invocation|
|PauseIfRequested()|Call this function inside the thread logic where you want to pause when user call Pause(...)|-|
|StopIfRequested()|Call this function inside the thread logic where you want to stop when user call Stop(...)|-|
|CheckIfRequestedStop()|Call this function inside the thread logic where you want to check if requested stop (if user call Stop(...))|-|
|Pause()|Pause all threads|-|
|Resume(bool resumeThreadPausedById = false)|Resume all threads|-|
|Pause(string id)|Pause thread by id|-|
|Resume(string id)|Resume thread by id|-|
|Cancel()|Cancel all tasks that have not started running|-|
|Cancel(string id)|Cancel the task by id if the task has not started running|is succeed|
### **API List**
### PowerPool
#### Properties
```csharp
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
bool Wait(string id)
async Task WaitAsync();
async Task<bool> WaitAsync(string id)
void Stop(bool forceStop = false);
async Task StopAsync(bool forceStop = false);
bool Stop(string id, bool forceStop = false);
async Task<bool> StopAsync(string id, bool forceStop = false);
void PauseIfRequested();
void StopIfRequested();
bool CheckIfRequestedStop();
void Pause();
void Resume(bool resumeThreadPausedById = false);
void Pause(string id);
void Resume(string id);
void Cancel();
bool Cancel(string id);
```
### ExcuteResult\<TResult>
#### Properties
```csharp
string ID // Get
TResult Result; // Get
Status Status; // Get
Exception Exception; // Get
```
### Status
```csharp
enum Status { Succeed, Failed }
```
### ThreadPoolOption
#### Properties
```csharp
int MaxThreads; // Get, Set
TimeoutOption Timeout; // Get, Set
TimeoutOption DefaultThreadTimeout; // Get, Set
Action<ExecuteResult<object>> DefaultCallback; // Get, Set
```
### ThreadOption
#### Properties
```csharp
TimeoutOption Timeout; // Get, Set
Action<ExecuteResult<TResult>> Callback; // Get, Set
int Priority; // Get, Set
DestroyThreadOption DestroyThreadOption; // Get, Set
```
### DestroyThreadOption
#### Properties
```csharp
int KeepAliveTime; // Get, Set
int MinThreads; // Get, Set
```