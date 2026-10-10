```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.256 μs | 0.2566 μs | 0.0141 μs |  1.00 |    0.01 | 0.0267 |    2.3 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.282 μs | 0.3321 μs | 0.0182 μs |  1.01 |    0.01 | 0.0267 |    2.3 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.768 μs | 1.0129 μs | 0.0555 μs |  1.23 |    0.02 | 0.0305 |   2.57 KB |        1.12 |
| Publish_Notification_WithOpenTelemetry             | 2.786 μs | 0.1893 μs | 0.0104 μs |  1.24 |    0.01 | 0.0305 |   2.57 KB |        1.12 |
