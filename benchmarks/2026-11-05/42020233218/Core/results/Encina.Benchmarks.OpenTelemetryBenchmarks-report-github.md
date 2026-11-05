```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.058 μs | 0.2538 μs | 0.0139 μs |  1.00 | 0.0267 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.058 μs | 0.3312 μs | 0.0182 μs |  1.00 | 0.0267 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.418 μs | 0.0529 μs | 0.0029 μs |  1.18 | 0.0305 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 2.461 μs | 0.0698 μs | 0.0038 μs |  1.20 | 0.0305 |   2.57 KB |        1.17 |
