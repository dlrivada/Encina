```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.697 μs | 0.0275 μs | 0.0411 μs |  1.00 |    0.02 | 0.1373 |    2.3 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.776 μs | 0.0107 μs | 0.0154 μs |  1.03 |    0.02 | 0.1373 |    2.3 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.106 μs | 0.0131 μs | 0.0183 μs |  1.15 |    0.02 | 0.1564 |   2.57 KB |        1.12 |
| Publish_Notification_WithOpenTelemetry             | 3.036 μs | 0.0163 μs | 0.0238 μs |  1.13 |    0.02 | 0.1564 |   2.57 KB |        1.12 |
