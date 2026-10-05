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
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.509 μs | 0.0711 μs | 0.0039 μs |  1.00 | 0.0267 |   2.29 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.482 μs | 0.3969 μs | 0.0218 μs |  0.99 | 0.0267 |   2.29 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.076 μs | 0.2265 μs | 0.0124 μs |  1.23 | 0.0305 |   2.56 KB |        1.12 |
| Publish_Notification_WithOpenTelemetry             | 3.062 μs | 0.1307 μs | 0.0072 μs |  1.22 | 0.0305 |   2.56 KB |        1.12 |
