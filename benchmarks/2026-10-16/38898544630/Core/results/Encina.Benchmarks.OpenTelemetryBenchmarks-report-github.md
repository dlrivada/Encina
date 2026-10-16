```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.013 μs | 0.5405 μs | 0.0296 μs |  1.00 |    0.02 | 0.1373 |    2.3 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.018 μs | 0.1669 μs | 0.0091 μs |  1.00 |    0.01 | 0.1373 |    2.3 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.200 μs | 0.5742 μs | 0.0315 μs |  1.09 |    0.02 | 0.1564 |   2.57 KB |        1.12 |
| Publish_Notification_WithOpenTelemetry             | 2.116 μs | 0.4196 μs | 0.0230 μs |  1.05 |    0.02 | 0.1564 |   2.57 KB |        1.12 |
