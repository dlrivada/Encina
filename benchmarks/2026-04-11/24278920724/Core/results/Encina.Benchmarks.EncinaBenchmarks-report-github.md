```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error     | StdDev    | Gen0   | Allocated |
|------------------------------------------ |---------:|----------:|----------:|-------:|----------:|
| Publish_Notification_WithMultipleHandlers | 4.503 μs | 0.0232 μs | 0.0341 μs | 0.1984 |   3.35 KB |
| Send_Command_WithInstrumentation          | 5.765 μs | 0.0303 μs | 0.0444 μs | 0.2365 |   3.94 KB |
