```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 3.582 μs | 0.1941 μs | 0.0106 μs |  1.00 | 0.1373 |    2.3 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.816 μs | 0.1312 μs | 0.0072 μs |  1.07 | 0.1373 |    2.3 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 4.036 μs | 0.0787 μs | 0.0043 μs |  1.13 | 0.1526 |   2.57 KB |        1.12 |
| Publish_Notification_WithOpenTelemetry             | 3.985 μs | 0.1023 μs | 0.0056 μs |  1.11 | 0.1526 |   2.57 KB |        1.12 |
