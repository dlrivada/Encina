```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.976 μs | 0.1095 μs | 0.0060 μs |  1.00 | 0.0877 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.057 μs | 0.9231 μs | 0.0506 μs |  1.03 | 0.0877 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.454 μs | 0.2087 μs | 0.0114 μs |  1.16 | 0.1030 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 3.487 μs | 0.5648 μs | 0.0310 μs |  1.17 | 0.1030 |   2.57 KB |        1.17 |
