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
| Send_Request_Baseline_WithoutOpenTelemetry         | 82.28 ms |    NA |  1.00 |    7.7 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 83.77 ms |    NA |  1.02 |   9.42 KB |        1.22 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 69.54 ms |    NA |  0.85 |  12.16 KB |        1.58 |
| Publish_Notification_WithOpenTelemetry             | 69.35 ms |    NA |  0.84 |  12.63 KB |        1.64 |
