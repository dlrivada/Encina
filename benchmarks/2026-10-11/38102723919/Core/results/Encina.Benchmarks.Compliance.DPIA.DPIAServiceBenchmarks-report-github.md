```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.71GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1.632 μs | 0.0236 μs | 0.0353 μs |  1.00 |    0.03 |    3 | 0.0172 | 0.0153 |     464 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1.501 μs | 0.0264 μs | 0.0395 μs |  0.92 |    0.03 |    3 | 0.0172 | 0.0153 |     464 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             | 1.035 μs | 0.0178 μs | 0.0266 μs |  0.63 |    0.02 |    1 | 0.0114 | 0.0095 |     352 B |        0.76 |
| &#39;Request DPO consultation&#39;                           | 1.494 μs | 0.0405 μs | 0.0606 μs |  0.92 |    0.04 |    3 | 0.0172 | 0.0153 |     464 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2.510 μs | 0.0339 μs | 0.0497 μs |  1.54 |    0.04 |    4 | 0.0343 | 0.0305 |    1008 B |        2.17 |
| &#39;Reject assessment&#39;                                  | 1.666 μs | 0.0345 μs | 0.0516 μs |  1.02 |    0.04 |    3 | 0.0134 | 0.0114 |     400 B |        0.86 |
| &#39;Request revision&#39;                                   | 1.610 μs | 0.0692 μs | 0.1036 μs |  0.99 |    0.07 |    3 | 0.0134 | 0.0114 |     400 B |        0.86 |
| &#39;Expire assessment&#39;                                  | 1.363 μs | 0.0446 μs | 0.0667 μs |  0.84 |    0.04 |    2 | 0.0134 | 0.0114 |     384 B |        0.83 |
| &#39;Get assessment by ID (cached)&#39;                      | 1.394 μs | 0.0279 μs | 0.0418 μs |  0.85 |    0.03 |    2 | 0.0153 | 0.0134 |     456 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1.308 μs | 0.0234 μs | 0.0350 μs |  0.80 |    0.03 |    2 | 0.0153 | 0.0134 |     424 B |        0.91 |
| &#39;Get expired assessments&#39;                            | 1.078 μs | 0.0180 μs | 0.0258 μs |  0.66 |    0.02 |    1 | 0.0153 | 0.0134 |     416 B |        0.90 |
| &#39;Get all assessments&#39;                                | 1.002 μs | 0.0210 μs | 0.0308 μs |  0.61 |    0.02 |    1 | 0.0153 | 0.0134 |     416 B |        0.90 |
