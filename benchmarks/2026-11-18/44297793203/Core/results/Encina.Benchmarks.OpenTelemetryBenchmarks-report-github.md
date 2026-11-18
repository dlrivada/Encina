```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 3.020 μs | 0.6909 μs | 0.0379 μs |  1.00 |    0.02 | 0.1335 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.894 μs | 0.0575 μs | 0.0032 μs |  0.96 |    0.01 | 0.1335 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.253 μs | 0.1653 μs | 0.0091 μs |  1.08 |    0.01 | 0.1564 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 3.222 μs | 0.3394 μs | 0.0186 μs |  1.07 |    0.01 | 0.1564 |   2.57 KB |        1.17 |
