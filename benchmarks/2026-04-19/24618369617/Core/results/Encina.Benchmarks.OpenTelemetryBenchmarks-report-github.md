```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.527 μs | 0.0100 μs | 0.0146 μs |  1.00 |    0.01 | 0.0648 |   1.61 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.568 μs | 0.0186 μs | 0.0279 μs |  1.02 |    0.01 | 0.0648 |   1.61 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.386 μs | 0.0308 μs | 0.0442 μs |  0.94 |    0.02 | 0.0687 |   1.73 KB |        1.07 |
| Publish_Notification_WithOpenTelemetry             | 2.360 μs | 0.0039 μs | 0.0058 μs |  0.93 |    0.01 | 0.0687 |   1.73 KB |        1.07 |
