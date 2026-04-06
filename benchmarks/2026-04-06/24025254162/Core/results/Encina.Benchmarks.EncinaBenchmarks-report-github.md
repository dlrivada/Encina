```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Gen0   | Allocated |
|------------------------------------------ |---------:|----------:|----------:|-------:|----------:|
| Send_Command_WithInstrumentation          | 5.763 μs | 0.1468 μs | 0.0080 μs | 0.2365 |   3.94 KB |
| Publish_Notification_WithMultipleHandlers | 4.338 μs | 0.3171 μs | 0.0174 μs | 0.1984 |   3.35 KB |
