```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 1.650 μs | 0.4196 μs | 0.0230 μs |  1.00 |    0.02 | 0.0973 |   1.61 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 1.612 μs | 0.0928 μs | 0.0051 μs |  0.98 |    0.01 | 0.0973 |   1.61 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 1.110 μs | 0.1483 μs | 0.0081 μs |  0.67 |    0.01 | 0.1049 |   1.73 KB |        1.07 |
| Publish_Notification_WithOpenTelemetry             | 1.256 μs | 3.9296 μs | 0.2154 μs |  0.76 |    0.11 | 0.1049 |   1.73 KB |        1.07 |
