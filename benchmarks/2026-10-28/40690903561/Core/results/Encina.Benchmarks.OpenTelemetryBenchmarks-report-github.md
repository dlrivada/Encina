```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.515 μs | 0.2161 μs | 0.0118 μs |  1.00 |    0.01 | 0.0267 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.528 μs | 0.0684 μs | 0.0037 μs |  1.01 |    0.00 | 0.0267 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.010 μs | 0.3726 μs | 0.0204 μs |  1.20 |    0.01 | 0.0305 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 3.042 μs | 1.3471 μs | 0.0738 μs |  1.21 |    0.03 | 0.0305 |   2.57 KB |        1.17 |
