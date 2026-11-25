```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.36GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 1.898 μs | 0.2880 μs | 0.0158 μs |  1.00 | 0.0267 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 1.959 μs | 0.3605 μs | 0.0198 μs |  1.03 | 0.0267 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.219 μs | 0.3856 μs | 0.0211 μs |  1.17 | 0.0305 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 2.253 μs | 0.3170 μs | 0.0174 μs |  1.19 | 0.0305 |   2.57 KB |        1.17 |
