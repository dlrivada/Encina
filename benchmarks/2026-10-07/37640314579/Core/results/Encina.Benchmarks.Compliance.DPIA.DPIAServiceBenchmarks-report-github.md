```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,645.6 ns | 484.7 ns | 26.57 ns |  1.00 |    0.02 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,483.5 ns | 169.1 ns |  9.27 ns |  0.90 |    0.01 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   962.6 ns | 304.0 ns | 16.67 ns |  0.59 |    0.01 |    1 | 0.0114 | 0.0095 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;                           | 1,402.8 ns | 247.2 ns | 13.55 ns |  0.85 |    0.01 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,318.0 ns | 144.9 ns |  7.94 ns |  1.41 |    0.02 |    2 | 0.0343 | 0.0305 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,424.2 ns | 397.5 ns | 21.79 ns |  0.87 |    0.02 |    1 | 0.0134 | 0.0114 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,393.9 ns | 420.3 ns | 23.04 ns |  0.85 |    0.02 |    1 | 0.0134 | 0.0114 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,218.7 ns | 161.5 ns |  8.85 ns |  0.74 |    0.01 |    1 | 0.0134 | 0.0114 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,253.2 ns | 252.9 ns | 13.86 ns |  0.76 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,141.4 ns | 187.6 ns | 10.28 ns |  0.69 |    0.01 |    1 | 0.0153 | 0.0134 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            | 1,050.7 ns | 500.9 ns | 27.46 ns |  0.64 |    0.02 |    1 | 0.0153 | 0.0134 |     384 B |        0.89 |
| &#39;Get all assessments&#39;                                |   990.2 ns | 445.1 ns | 24.40 ns |  0.60 |    0.02 |    1 | 0.0153 | 0.0134 |     384 B |        0.89 |
