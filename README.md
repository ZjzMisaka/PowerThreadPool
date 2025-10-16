# PowerThreadPool
![icon](https://raw.githubusercontent.com/ZjzMisaka/PowerThreadPool/main/icon.png)

![Nuget](https://img.shields.io/nuget/v/PowerThreadPool?style=for-the-badge)
![Nuget](https://img.shields.io/nuget/dt/PowerThreadPool?style=for-the-badge)
![GitHub release (with filter)](https://img.shields.io/github/v/release/ZjzMisaka/PowerThreadPool?style=for-the-badge)
![GitHub Repo stars](https://img.shields.io/github/stars/ZjzMisaka/PowerThreadPool?style=for-the-badge)
![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/ZjzMisaka/PowerThreadPool/test.yml?style=for-the-badge)
[![Codecov](https://img.shields.io/codecov/c/github/ZjzMisaka/PowerThreadPool?style=for-the-badge)](https://app.codecov.io/gh/ZjzMisaka/PowerThreadPool)
[![CodeFactor](https://www.codefactor.io/repository/github/zjzmisaka/powerthreadpool/badge?style=for-the-badge)](https://www.codefactor.io/repository/github/zjzmisaka/powerthreadpool)

<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
[![All Contributors](https://img.shields.io/badge/all_contributors-7-orange.svg?style=flat-square)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->

A comprehensive and efficient low-contention thread pool for easily managing both sync and async workloads. It provides granular work control, flexible concurrency, and robust error handling.  

## Documentation
Access the Wiki in [English](https://github.com/ZjzMisaka/PowerThreadPool/wiki) | [中文](https://github.com/ZjzMisaka/PowerThreadPool.zh-CN.Wiki/wiki) | [日本語](https://github.com/ZjzMisaka/PowerThreadPool.ja-JP.Wiki/wiki).  
Visit the [DeepWiki](https://deepwiki.com/ZjzMisaka/PowerThreadPool) for more information.  

## Pack
```ps1
powershell -File build.ps1 -Version {Version}
```

## Installation
If you want to include PowerThreadPool in your project, you can [install it directly from NuGet](https://www.nuget.org/packages/PowerThreadPool/).  
Support: Net40+ | Net5.0+ | netstandard2.0+  

## Features
- [Sync | Async](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Sync-Async)
- [Pool Control | Work Control](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control)
    - [Stop](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Pause](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Resume](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Force Stop](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#force-stop)
    - [Wait](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#wait)
    - [Fetch](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#fetch)
    - [Cancel](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#cancel)
- [Divide And Conquer](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Divide-And-Conquer)
- [Thread Pool Sizing](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Thread-Pool-Sizing)
    - [Idle Thread Scheduled Destruction](https://github.com/ZjzMisaka/PowerThreadPool/wiki/DestroyThreadOption)
    - [Thread Starvation Countermeasures (Long-running Work Support)](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Thread-Pool-Sizing#thread-starvation)
- [Work Callback | Default Callback](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Callback)
- [Rejection Policy](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Rejection-Policy)
- [Parallel Execution](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Parallel-Execution)
    - [For](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Parallel-Execution#For)
    - [ForEach](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Parallel-Execution#ForEach)
    - [Watch](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Parallel-Execution#Watch)
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
- [Running Timer](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Running-Timer)
- [Queue Type (FIFO | LIFO | Custom)](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Queue-Type)
- [Load Balancing](https://en.wikipedia.org/wiki/Work_stealing)
- [Low-Contention Design](https://en.wikipedia.org/wiki/Non-blocking_algorithm)

## Getting started
### Out-of-the-box: Run a simple work
PTP is designed to be out-of-the-box. For simple works, you can get started without any complex configuration.  
```csharp
PowerPool powerPool = new PowerPool();
// Sync
powerPool.QueueWorkItem(() => 
{
    // Do something
});
// Async
powerPool.QueueWorkItemAsync(async () =>
{
    // Do something
    // await ...;
});
```

### With callback
```csharp
PowerPool powerPool = new PowerPool(new PowerPoolOption() { /* Some options */ });
// Sync
powerPool.QueueWorkItem(() => 
{
    // Do something
    return result;
}, (res) => 
{
    // Callback of the work
});
// Async
powerPool.QueueWorkItemAsync(async () =>
{
    // Do something
    // await ...;
}, (res) =>
{
    // Callback of the work
});
```

### With option
```csharp
PowerPool powerPool = new PowerPool(new PowerPoolOption() { /* Some options */ });
// Sync
powerPool.QueueWorkItem(() => 
{
    // Do something
    return result;
}, new WorkOption()
{
    // Some options
});
// Async
powerPool.QueueWorkItemAsync(async () =>
{
    // Do something
    // await ...;
}, new WorkOption()
{
    // Some options
});
```

### Reference
``` csharp
WorkID QueueWorkItem<T1, ...>(Action<T1, ...> action, T1 param1, ..., *);
WorkID QueueWorkItem(Action action, *);
WorkID QueueWorkItem(Action<object[]> action, object[] param, *);
WorkID QueueWorkItem<T1, ..., TResult>(Func<T1, ..., TResult> function, T1 param1, ..., *);
WorkID QueueWorkItem<TResult>(Func<TResult> function, *);
WorkID QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, *);
WorkID QueueWorkItemAsync(Func<Task> asyncFunc, *);
WorkID QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, *);
WorkID QueueWorkItemAsync(Func<Task> asyncFunc, out Task task, *);
WorkID QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, out Task<ExecuteResult<TResult>> task, *);
```
- Asterisk (*) denotes an optional parameter, either a WorkOption or a delegate (`Action<ExecuteResult<object>>` or `Action<ExecuteResult<TResult>>`), depending on whether the first parameter is an Action or a Func. 
- In places where you see ellipses (...), you can provide up to five generic type parameters.

## More
[Testing And Performance Analysis](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Testing-And-Performance-Analysis) | [Feature Comparison](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Feature-Comparison)  
**Get involved**: [Join our growing community](https://github.com/ZjzMisaka/PowerThreadPool/discussions/258)  

## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/dlnn"><img src="https://avatars.githubusercontent.com/u/22004270?v=4?s=100" width="100px;" alt="一条咸鱼"/><br /><sub><b>一条咸鱼</b></sub></a><br /><a href="https://github.com/ZjzMisaka/PowerThreadPool/commits?author=dlnn" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/ZjzMisaka"><img src="https://avatars.githubusercontent.com/u/16731853?v=4?s=100" width="100px;" alt="ZjzMisaka"/><br /><sub><b>ZjzMisaka</b></sub></a><br /><a href="https://github.com/ZjzMisaka/PowerThreadPool/commits?author=ZjzMisaka" title="Code">💻</a> <a href="#maintenance-ZjzMisaka" title="Maintenance">🚧</a> <a href="https://github.com/ZjzMisaka/PowerThreadPool/commits?author=ZjzMisaka" title="Documentation">📖</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/r00tee"><img src="https://avatars.githubusercontent.com/u/32619657?v=4?s=100" width="100px;" alt="r00tee"/><br /><sub><b>r00tee</b></sub></a><br /><a href="#ideas-r00tee" title="Ideas, Planning, & Feedback">🤔</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/aadog"><img src="https://avatars.githubusercontent.com/u/18098725?v=4?s=100" width="100px;" alt="aadog"/><br /><sub><b>aadog</b></sub></a><br /><a href="https://github.com/ZjzMisaka/PowerThreadPool/issues?q=author%3Aaadog" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/RookieZWH"><img src="https://avatars.githubusercontent.com/u/17580767?v=4?s=100" width="100px;" alt="RookieZWH"/><br /><sub><b>RookieZWH</b></sub></a><br /><a href="#question-RookieZWH" title="Answering Questions">💬</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/hebinary"><img src="https://avatars.githubusercontent.com/u/86285187?v=4?s=100" width="100px;" alt="hebinary"/><br /><sub><b>hebinary</b></sub></a><br /><a href="#question-hebinary" title="Answering Questions">💬</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://blog.lindexi.com/"><img src="https://avatars.githubusercontent.com/u/16054566?v=4?s=100" width="100px;" alt="lindexi"/><br /><sub><b>lindexi</b></sub></a><br /><a href="https://github.com/ZjzMisaka/PowerThreadPool/issues?q=author%3Alindexi" title="Bug reports">🐛</a></td>
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!