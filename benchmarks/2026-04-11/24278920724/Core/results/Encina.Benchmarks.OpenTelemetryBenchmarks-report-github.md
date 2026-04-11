```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Send_Request_Baseline_WithoutOpenTelemetry         | 3.194 μs | 0.0405 μs | 0.0594 μs |  1.00 |    0.03 | 0.0954 |   1.61 KB |        1.00 |
| Send_Request_WithOpenTelemetry                     | 3.130 μs | 0.0204 μs | 0.0305 μs |  0.98 |    0.02 | 0.0954 |   1.61 KB |        1.00 |
| Publish_Notification_Baseline_WithoutOpenTelemetry | 2.322 μs | 0.0188 μs | 0.0276 μs |  0.73 |    0.02 | 0.1030 |   1.73 KB |        1.07 |
| Publish_Notification_WithOpenTelemetry             | 2.323 μs | 0.0196 μs | 0.0281 μs |  0.73 |    0.02 | 0.1030 |   1.73 KB |        1.07 |
