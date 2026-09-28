```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.523 μs | 0.2084 μs | 0.0114 μs |  1.00 | 0.1183 |   1.97 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.510 μs | 0.3445 μs | 0.0189 μs |  0.99 | 0.1183 |   1.97 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.012 μs | 0.3337 μs | 0.0183 μs |  1.19 | 0.1335 |   2.24 KB |        1.14 |
| Publish_Notification_WithOpenTelemetry             | 2.881 μs | 0.5420 μs | 0.0297 μs |  1.14 | 0.1335 |   2.24 KB |        1.14 |
