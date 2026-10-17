```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.946 μs | 0.1160 μs | 0.0064 μs |  1.00 | 0.0877 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.983 μs | 0.4811 μs | 0.0264 μs |  1.01 | 0.0877 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.441 μs | 0.2977 μs | 0.0163 μs |  1.17 | 0.1030 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 3.395 μs | 0.2772 μs | 0.0152 μs |  1.15 | 0.1030 |   2.57 KB |        1.17 |
