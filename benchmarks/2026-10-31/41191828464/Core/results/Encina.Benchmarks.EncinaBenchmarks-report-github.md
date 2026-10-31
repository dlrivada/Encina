```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Gen0   | Allocated |
|------------------------------------------ |---------:|----------:|----------:|-------:|----------:|
| Publish_Notification_WithMultipleHandlers | 3.681 μs | 0.1724 μs | 0.0095 μs | 0.2556 |    4.2 KB |
| Send_Command_WithInstrumentation          | 4.583 μs | 0.5833 μs | 0.0320 μs | 0.2823 |    4.7 KB |
