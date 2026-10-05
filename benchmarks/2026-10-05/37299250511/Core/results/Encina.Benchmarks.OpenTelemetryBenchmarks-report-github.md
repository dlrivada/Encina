```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.160 μs | 0.0780 μs | 0.0043 μs |  1.00 | 0.0229 |   1.97 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.179 μs | 0.0369 μs | 0.0020 μs |  1.01 | 0.0229 |   1.97 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.787 μs | 0.1933 μs | 0.0106 μs |  1.29 | 0.0267 |   2.24 KB |        1.14 |
| Publish_Notification_WithOpenTelemetry             | 2.855 μs | 0.5084 μs | 0.0279 μs |  1.32 | 0.0267 |   2.24 KB |        1.14 |
