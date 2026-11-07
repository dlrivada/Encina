```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.508 μs | 0.0847 μs | 0.0046 μs |  1.00 | 0.0267 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.584 μs | 0.7877 μs | 0.0432 μs |  1.03 | 0.0267 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.988 μs | 0.4164 μs | 0.0228 μs |  1.19 | 0.0305 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 3.043 μs | 0.0676 μs | 0.0037 μs |  1.21 | 0.0305 |   2.57 KB |        1.17 |
