```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.590 μs | 0.0366 μs | 0.0020 μs |  1.00 | 0.0267 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.640 μs | 0.1304 μs | 0.0071 μs |  1.02 | 0.0267 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.091 μs | 0.2599 μs | 0.0142 μs |  1.19 | 0.0305 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 3.082 μs | 0.1373 μs | 0.0075 μs |  1.19 | 0.0305 |   2.57 KB |        1.17 |
