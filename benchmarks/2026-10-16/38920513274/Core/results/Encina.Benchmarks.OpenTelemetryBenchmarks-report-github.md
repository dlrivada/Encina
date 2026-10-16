```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 3.760 μs | 0.2172 μs | 0.0119 μs |  1.00 | 0.1373 |    2.3 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.691 μs | 0.1278 μs | 0.0070 μs |  0.98 | 0.1373 |    2.3 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 4.078 μs | 0.3506 μs | 0.0192 μs |  1.08 | 0.1526 |   2.57 KB |        1.12 |
| Publish_Notification_WithOpenTelemetry             | 4.059 μs | 0.3074 μs | 0.0168 μs |  1.08 | 0.1526 |   2.57 KB |        1.12 |
