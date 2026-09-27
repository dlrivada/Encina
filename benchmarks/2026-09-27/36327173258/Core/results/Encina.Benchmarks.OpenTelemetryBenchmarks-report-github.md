```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.647 μs | 0.1525 μs | 0.0084 μs |  1.00 |    0.00 | 0.0801 |   1.97 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.651 μs | 0.2576 μs | 0.0141 μs |  1.00 |    0.01 | 0.0801 |   1.97 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.463 μs | 0.1026 μs | 0.0056 μs |  1.31 |    0.00 | 0.0877 |   2.24 KB |        1.14 |
| Publish_Notification_WithOpenTelemetry             | 3.265 μs | 0.8793 μs | 0.0482 μs |  1.23 |    0.02 | 0.0877 |   2.24 KB |        1.14 |
