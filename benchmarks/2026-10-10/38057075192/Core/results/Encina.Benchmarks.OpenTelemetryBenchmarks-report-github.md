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
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.466 μs | 0.0675 μs | 0.0037 μs |  1.00 | 0.0267 |    2.3 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.485 μs | 0.1359 μs | 0.0074 μs |  1.01 | 0.0267 |    2.3 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.036 μs | 0.4659 μs | 0.0255 μs |  1.23 | 0.0305 |   2.57 KB |        1.12 |
| Publish_Notification_WithOpenTelemetry             | 3.057 μs | 0.3776 μs | 0.0207 μs |  1.24 | 0.0305 |   2.57 KB |        1.12 |
