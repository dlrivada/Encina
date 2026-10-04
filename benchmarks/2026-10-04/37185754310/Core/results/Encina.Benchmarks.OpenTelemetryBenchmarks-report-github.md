```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 3.169 μs | 0.2813 μs | 0.0154 μs |  1.00 | 0.1183 |   1.97 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.148 μs | 0.3433 μs | 0.0188 μs |  0.99 | 0.1183 |   1.97 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 3.545 μs | 0.2232 μs | 0.0122 μs |  1.12 | 0.1335 |   2.24 KB |        1.14 |
| Publish_Notification_WithOpenTelemetry             | 3.574 μs | 0.0415 μs | 0.0023 μs |  1.13 | 0.1335 |   2.24 KB |        1.14 |
