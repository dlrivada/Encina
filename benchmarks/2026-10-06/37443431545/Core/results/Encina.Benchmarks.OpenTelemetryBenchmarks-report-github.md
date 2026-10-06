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
| Send_Request_Baseline_WithoutOpenTelemetry         | 3.179 μs | 0.1018 μs | 0.0056 μs |  1.00 | 0.1373 |   2.29 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.277 μs | 0.1625 μs | 0.0089 μs |  1.03 | 0.1373 |   2.29 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.664 μs | 0.1282 μs | 0.0070 μs |  1.15 | 0.1564 |   2.56 KB |        1.12 |
| Publish_Notification_WithOpenTelemetry             | 3.753 μs | 0.2646 μs | 0.0145 μs |  1.18 | 0.1564 |   2.56 KB |        1.12 |
