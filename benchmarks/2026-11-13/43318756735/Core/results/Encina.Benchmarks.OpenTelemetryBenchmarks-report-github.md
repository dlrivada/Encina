```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 1.979 μs | 0.6178 μs | 0.0339 μs |  1.00 |    0.02 | 0.1335 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.013 μs | 1.0923 μs | 0.0599 μs |  1.02 |    0.03 | 0.1335 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.108 μs | 0.4405 μs | 0.0241 μs |  1.07 |    0.02 | 0.1564 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 2.089 μs | 0.4653 μs | 0.0255 μs |  1.06 |    0.02 | 0.1564 |   2.57 KB |        1.17 |
