```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Gen0   | Allocated |
|------------------------------------------ |---------:|----------:|----------:|-------:|----------:|
| Publish_Notification_WithMultipleHandlers | 4.653 μs | 1.5443 μs | 0.0846 μs | 0.1984 |   3.35 KB |
| Send_Command_WithInstrumentation          | 5.904 μs | 0.4191 μs | 0.0230 μs | 0.2365 |   3.94 KB |
