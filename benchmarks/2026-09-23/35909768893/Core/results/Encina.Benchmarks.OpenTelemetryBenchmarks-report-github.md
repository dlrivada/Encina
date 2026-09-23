```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 3.121 μs | 2.0885 μs | 0.1145 μs |  1.00 |    0.04 | 0.1183 |   1.97 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.136 μs | 0.2340 μs | 0.0128 μs |  1.01 |    0.03 | 0.1183 |   1.97 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.643 μs | 0.2142 μs | 0.0117 μs |  1.17 |    0.04 | 0.1335 |   2.24 KB |        1.14 |
| Publish_Notification_WithOpenTelemetry             | 3.599 μs | 0.3359 μs | 0.0184 μs |  1.15 |    0.04 | 0.1335 |   2.24 KB |        1.14 |
