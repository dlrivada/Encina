```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.70GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 2.522 μs | 0.0085 μs | 0.0125 μs |  1.00 | 0.0648 |   1.61 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 2.597 μs | 0.0143 μs | 0.0214 μs |  1.03 | 0.0648 |   1.61 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.266 μs | 0.0130 μs | 0.0191 μs |  0.90 | 0.0687 |   1.73 KB |        1.07 |
| Publish_Notification_WithOpenTelemetry             | 2.292 μs | 0.0042 μs | 0.0060 μs |  0.91 | 0.0687 |   1.73 KB |        1.07 |
