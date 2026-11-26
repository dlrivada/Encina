```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.226 μs | 1.5914 μs | 0.0872 μs |  1.00 |    0.05 | 0.1335 |    2.2 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.188 μs | 0.1909 μs | 0.0105 μs |  0.98 |    0.03 | 0.1335 |    2.2 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.370 μs | 0.2295 μs | 0.0126 μs |  1.07 |    0.04 | 0.1564 |   2.57 KB |        1.17 |
| Publish_Notification_WithOpenTelemetry             | 2.275 μs | 0.6954 μs | 0.0381 μs |  1.02 |    0.04 | 0.1564 |   2.57 KB |        1.17 |
