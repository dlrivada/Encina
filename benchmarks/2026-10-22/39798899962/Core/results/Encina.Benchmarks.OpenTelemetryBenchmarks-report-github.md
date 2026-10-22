```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 3.672 μs | 0.3693 μs | 0.0202 μs |  1.00 | 0.1335 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.695 μs | 0.0514 μs | 0.0028 μs |  1.01 | 0.1335 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.932 μs | 0.3146 μs | 0.0172 μs |  1.07 | 0.1526 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 4.043 μs | 0.9281 μs | 0.0509 μs |  1.10 | 0.1526 |   2.57 KB |        1.17 |
