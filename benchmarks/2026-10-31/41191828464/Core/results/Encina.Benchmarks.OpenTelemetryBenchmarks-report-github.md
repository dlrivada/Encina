```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 3.711 μs | 0.9212 μs | 0.0505 μs |  1.00 |    0.02 | 0.1335 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.690 μs | 0.4066 μs | 0.0223 μs |  0.99 |    0.01 | 0.1335 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.881 μs | 0.4829 μs | 0.0265 μs |  1.05 |    0.01 | 0.1526 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 3.958 μs | 0.1312 μs | 0.0072 μs |  1.07 |    0.01 | 0.1526 |   2.57 KB |        1.17 |
