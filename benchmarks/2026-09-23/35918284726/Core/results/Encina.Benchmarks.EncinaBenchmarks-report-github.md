```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Gen0   | Allocated |
|------------------------------------------ |---------:|----------:|----------:|-------:|----------:|
| Publish_Notification_WithMultipleHandlers | 5.715 μs | 0.0803 μs | 0.0044 μs | 0.2365 |   3.87 KB |
| Send_Command_WithInstrumentation          | 6.179 μs | 0.2160 μs | 0.0118 μs | 0.2594 |    4.3 KB |
