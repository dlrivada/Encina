```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.102 μs | 1.5865 μs | 0.0870 μs |  1.00 |    0.05 | 0.1373 |    2.3 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.101 μs | 2.4031 μs | 0.1317 μs |  1.00 |    0.06 | 0.1373 |    2.3 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.316 μs | 0.1007 μs | 0.0055 μs |  1.10 |    0.04 | 0.1564 |   2.57 KB |        1.12 |
| Publish_Notification_WithOpenTelemetry             | 2.350 μs | 0.1304 μs | 0.0071 μs |  1.12 |    0.04 | 0.1564 |   2.57 KB |        1.12 |
