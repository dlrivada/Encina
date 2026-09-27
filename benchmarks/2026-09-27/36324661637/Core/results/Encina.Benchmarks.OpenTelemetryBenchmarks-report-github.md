```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 1.552 μs | 0.2571 μs | 0.0141 μs |  1.00 |    0.01 | 0.0229 |   1.97 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 1.624 μs | 0.8563 μs | 0.0469 μs |  1.05 |    0.03 | 0.0229 |   1.97 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.016 μs | 0.1330 μs | 0.0073 μs |  1.30 |    0.01 | 0.0267 |   2.24 KB |        1.14 |
| Publish_Notification_WithOpenTelemetry             | 2.034 μs | 0.4811 μs | 0.0264 μs |  1.31 |    0.02 | 0.0267 |   2.24 KB |        1.14 |
