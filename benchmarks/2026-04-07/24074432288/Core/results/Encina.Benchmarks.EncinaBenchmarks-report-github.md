```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Median   | Gen0   | Allocated |
|------------------------------------------ |---------:|----------:|----------:|---------:|-------:|----------:|
| Publish_Notification_WithMultipleHandlers | 4.390 μs | 0.0817 μs | 0.1198 μs | 4.296 μs | 0.1984 |   3.35 KB |
| Send_Command_WithInstrumentation          | 5.797 μs | 0.0945 μs | 0.1325 μs | 5.897 μs | 0.2365 |   3.94 KB |
