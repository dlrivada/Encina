```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.921 μs | 0.1197 μs | 0.0066 μs |  1.00 | 0.0954 |   1.61 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.960 μs | 0.1657 μs | 0.0091 μs |  1.01 | 0.0954 |   1.61 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.264 μs | 0.0651 μs | 0.0036 μs |  0.77 | 0.1030 |   1.73 KB |        1.07 |
| Publish_Notification_WithOpenTelemetry             | 2.316 μs | 0.3092 μs | 0.0170 μs |  0.79 | 0.1030 |   1.73 KB |        1.07 |
