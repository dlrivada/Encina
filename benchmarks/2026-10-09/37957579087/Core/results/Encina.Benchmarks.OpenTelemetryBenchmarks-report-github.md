```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 3.088 μs | 0.0449 μs | 0.0025 μs |  1.00 | 0.0916 |    2.3 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.106 μs | 0.1517 μs | 0.0083 μs |  1.01 | 0.0916 |    2.3 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.626 μs | 0.7016 μs | 0.0385 μs |  1.17 | 0.1030 |   2.57 KB |        1.12 |
| Publish_Notification_WithOpenTelemetry             | 3.527 μs | 0.3953 μs | 0.0217 μs |  1.14 | 0.1030 |   2.57 KB |        1.12 |
