```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 3.407 μs | 0.1886 μs | 0.0103 μs |  1.00 | 0.1373 |    2.3 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.504 μs | 0.0387 μs | 0.0021 μs |  1.03 | 0.1373 |    2.3 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.943 μs | 0.4880 μs | 0.0267 μs |  1.16 | 0.1526 |   2.57 KB |        1.12 |
| Publish_Notification_WithOpenTelemetry             | 3.862 μs | 0.1552 μs | 0.0085 μs |  1.13 | 0.1526 |   2.57 KB |        1.12 |
