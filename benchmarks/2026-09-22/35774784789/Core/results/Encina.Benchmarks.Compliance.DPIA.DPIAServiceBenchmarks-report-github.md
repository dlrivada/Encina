```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,647.3 ns | 424.06 ns | 23.24 ns |  1.00 |    0.02 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,504.6 ns | 204.87 ns | 11.23 ns |  0.91 |    0.01 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   984.4 ns |  76.76 ns |  4.21 ns |  0.60 |    0.01 |    1 | 0.0114 | 0.0095 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;                           | 1,429.6 ns | 323.12 ns | 17.71 ns |  0.87 |    0.01 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,444.6 ns | 738.85 ns | 40.50 ns |  1.48 |    0.03 |    2 | 0.0343 | 0.0305 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,425.7 ns | 545.83 ns | 29.92 ns |  0.87 |    0.02 |    1 | 0.0134 | 0.0114 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,403.6 ns | 427.90 ns | 23.45 ns |  0.85 |    0.02 |    1 | 0.0134 | 0.0114 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,229.6 ns | 308.33 ns | 16.90 ns |  0.75 |    0.01 |    1 | 0.0134 | 0.0114 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,264.1 ns | 476.93 ns | 26.14 ns |  0.77 |    0.02 |    1 | 0.0153 | 0.0134 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,180.7 ns |  24.52 ns |  1.34 ns |  0.72 |    0.01 |    1 | 0.0153 | 0.0134 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            | 1,084.7 ns | 555.68 ns | 30.46 ns |  0.66 |    0.02 |    1 | 0.0153 | 0.0134 |     384 B |        0.89 |
| &#39;Get all assessments&#39;                                | 1,028.4 ns | 541.60 ns | 29.69 ns |  0.62 |    0.02 |    1 | 0.0153 | 0.0134 |     384 B |        0.89 |
