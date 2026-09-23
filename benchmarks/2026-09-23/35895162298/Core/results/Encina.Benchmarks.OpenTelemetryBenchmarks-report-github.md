```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.737 μs | 0.8414 μs | 0.0461 μs |  1.00 |    0.02 | 0.0954 |   1.61 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.801 μs | 0.7963 μs | 0.0436 μs |  1.02 |    0.02 | 0.0954 |   1.61 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.160 μs | 0.2862 μs | 0.0157 μs |  0.79 |    0.01 | 0.1030 |   1.73 KB |        1.07 |
| Publish_Notification_WithOpenTelemetry             | 2.200 μs | 1.0024 μs | 0.0549 μs |  0.80 |    0.02 | 0.1030 |   1.73 KB |        1.07 |
