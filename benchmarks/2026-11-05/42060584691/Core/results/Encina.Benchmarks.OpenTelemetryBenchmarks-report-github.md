```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.795 μs | 0.1819 μs | 0.0100 μs |  1.00 |    0.00 | 0.1335 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.876 μs | 0.4961 μs | 0.0272 μs |  1.03 |    0.01 | 0.1335 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.135 μs | 0.6676 μs | 0.0366 μs |  1.12 |    0.01 | 0.1564 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 3.122 μs | 0.8738 μs | 0.0479 μs |  1.12 |    0.02 | 0.1564 |   2.57 KB |        1.17 |
