```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.506 μs | 0.0073 μs | 0.0110 μs |  1.00 | 0.0648 |   1.61 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.496 μs | 0.0033 μs | 0.0048 μs |  1.00 | 0.0648 |   1.61 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.251 μs | 0.0049 μs | 0.0072 μs |  0.90 | 0.0687 |   1.73 KB |        1.07 |
| Publish_Notification_WithOpenTelemetry             | 2.295 μs | 0.0113 μs | 0.0170 μs |  0.92 | 0.0687 |   1.73 KB |        1.07 |
