```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 3.100 μs | 0.4644 μs | 0.0255 μs |  1.00 | 0.0954 |   1.61 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.123 μs | 0.1300 μs | 0.0071 μs |  1.01 | 0.0954 |   1.61 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.227 μs | 0.2033 μs | 0.0111 μs |  0.72 | 0.1030 |   1.73 KB |        1.07 |
| Publish_Notification_WithOpenTelemetry             | 2.243 μs | 0.1558 μs | 0.0085 μs |  0.72 | 0.1030 |   1.73 KB |        1.07 |
