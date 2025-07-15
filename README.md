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
[![All Contributors](https://img.shields.io/badge/all_contributors-1-orange.svg?style=for-the-badge)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->

A comprehensive and efficient lock-free thread pool with granular work control, flexible concurrency, and robust error handling, alongside an easy-to-use API for diverse work submissions.  

## Documentation
Access the Wiki in [English](https://github.com/ZjzMisaka/PowerThreadPool/wiki) | [中文](https://github.com/ZjzMisaka/PowerThreadPool.zh-CN.Wiki/wiki) | [日本語](https://github.com/ZjzMisaka/PowerThreadPool.ja-JP.Wiki/wiki).  
Visit the [DeepWiki](https://deepwiki.com/ZjzMisaka/PowerThreadPool) for more information.  

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

## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/ZjzMisaka"><img src="https://avatars.githubusercontent.com/u/16731853?v=4?s=100" width="100px;" alt="ZjzMisaka"/><br /><sub><b>ZjzMisaka</b></sub></a><br /><a href="https://github.com/ZjzMisaka/PowerThreadPool/commits?author=ZjzMisaka" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/dlnn"><img src="https://avatars.githubusercontent.com/u/22004270?v=4?s=100" width="100px;" alt="一条咸鱼"/><br /><sub><b>一条咸鱼</b></sub></a><br /><a href="https://github.com/ZjzMisaka/PowerThreadPool/commits?author=dlnn" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/r00tee"><img src="https://avatars.githubusercontent.com/u/32619657?v=4?s=100" width="100px;" alt="r00tee"/><br /><sub><b>r00tee</b></sub></a><br /><a href="#ideas-r00tee" title="Ideas, Planning, & Feedback">🤔</a></td>
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!