```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                             | Mean     | Error | Ratio | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|------:|------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 79.41 ms |    NA |  1.00 |   6.66 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 79.33 ms |    NA |  1.00 |   7.34 KB |        1.10 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 65.71 ms |    NA |  0.83 |  13.51 KB |        2.03 |
| Publish_Notification_WithOpenTelemetry             | 66.31 ms |    NA |  0.84 |  12.24 KB |        1.84 |
