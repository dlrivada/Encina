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
| Send_Request_Baseline_WithoutOpenTelemetry         | 1.712 μs | 0.1832 μs | 0.0100 μs |  1.00 |    0.01 | 0.1202 |   1.97 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 1.808 μs | 0.4549 μs | 0.0249 μs |  1.06 |    0.01 | 0.1202 |   1.97 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.037 μs | 0.3411 μs | 0.0187 μs |  1.19 |    0.01 | 0.1335 |   2.24 KB |        1.14 |
| Publish_Notification_WithOpenTelemetry             | 1.954 μs | 0.5921 μs | 0.0325 μs |  1.14 |    0.02 | 0.1335 |   2.24 KB |        1.14 |
