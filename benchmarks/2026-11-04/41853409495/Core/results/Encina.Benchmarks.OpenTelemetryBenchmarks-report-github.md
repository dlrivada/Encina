```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.977 μs | 0.0580 μs | 0.0032 μs |  1.00 | 0.0877 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.027 μs | 0.1494 μs | 0.0082 μs |  1.02 | 0.0877 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.484 μs | 0.6955 μs | 0.0381 μs |  1.17 | 0.1030 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 3.552 μs | 0.0362 μs | 0.0020 μs |  1.19 | 0.1030 |   2.57 KB |        1.17 |
