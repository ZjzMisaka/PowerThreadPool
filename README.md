# PowerThreadPool
<img src="https://www.nuget.org/Content/gallery/img/logo-header.svg?sanitize=true" height="30px">

Enables efficient ThreadPool management with callback implementation, granular control, customizable concurrency, and support for diverse task submissions.

## Download
PowerThreadPool is available as [Nuget Package](https://www.nuget.org/packages/PowerThreadPool/) now.

## Getting started
### Without callback
```csharp
PowerPool powerPool = new PowerPool(new ThreadPoolOption() { MaxThreads = 3 });
powerPool.QueueWorkItem(() => 
{
    // DO SOMETHING
    return result;
});
```
### With callback
```csharp
PowerPool powerPool = new PowerPool(new ThreadPoolOption() { MaxThreads = 3 });
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
### Stop all threads
```csharp
powerPool.Stop();
```
### Blocks the calling thread until all of the threads terminates.
```csharp
powerPool.Wait();
```
### Pause threads
```csharp
PowerPool powerPool = new PowerPool(new ThreadPoolOption());
string id = powerPool.QueueWorkItem(() => 
{
    while (true)
    {
        // Pause here when user call Pause()
        powerPool.PauseIfRequested();
        // DO SOMETHING
    }
});
// DO SOMETHING
powerPool.Pause(id); // Pause by ID
powerPool.Pause(); // Pause all running thread
powerPool.Resume(true); // Resume all thread
```
### **API Summary**
### PowerPool
|name|summary|result|
|---|---|---|
|QueueWorkItem<...>(...)|Queues a method for execution. The method executes when a thread pool thread becomes available.|thread id|
|Wait()|Blocks the calling thread until all of the threads terminates.|-|
|WaitAsync()|Blocks the calling thread until all of the threads terminates.|Task|
|Stop(bool forceStop = false)|Stop all threads|-|
|StopAsync(bool forceStop = false)|Stop all threads|Task|
|Stop(string id, bool forceStop = false)|Stop thread by id|If thread is in progress during the invocation|
|StopAsync(string id, bool forceStop = false)|Stop thread by id|(Task) If thread is in progress during the invocation|
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
ThreadPoolOption ThreadPoolOption; // Get, Set
int WaitingThreadCount; // Get
IEnumerable<string> WaitingThreadList; // Get
int RunningThreadCount; // Get
IEnumerable<string> RunningThreadList; // Get
```
#### Events
```csharp
event ThreadPoolStartEventHandler ThreadPoolStart;
event IdleEventHandler Idle;
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
async Task WaitAsync();
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
```
