```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 1.649 μs | 0.3156 μs | 0.0173 μs |  1.00 |    0.01 | 0.0229 |   1.97 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 1.584 μs | 0.2306 μs | 0.0126 μs |  0.96 |    0.01 | 0.0229 |   1.97 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.036 μs | 1.4933 μs | 0.0819 μs |  1.24 |    0.04 | 0.0267 |   2.24 KB |        1.14 |
| Publish_Notification_WithOpenTelemetry             | 2.024 μs | 0.1645 μs | 0.0090 μs |  1.23 |    0.01 | 0.0267 |   2.24 KB |        1.14 |
