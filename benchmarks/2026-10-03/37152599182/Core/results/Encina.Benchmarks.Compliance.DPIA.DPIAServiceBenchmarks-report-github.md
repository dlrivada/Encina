```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,655.9 ns | 297.71 ns | 16.32 ns |  1.00 |    0.01 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,503.9 ns | 204.40 ns | 11.20 ns |  0.91 |    0.01 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   987.1 ns | 161.29 ns |  8.84 ns |  0.60 |    0.01 |    1 | 0.0114 | 0.0095 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;                           | 1,433.4 ns | 230.56 ns | 12.64 ns |  0.87 |    0.01 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,431.2 ns | 436.63 ns | 23.93 ns |  1.47 |    0.02 |    2 | 0.0343 | 0.0305 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,410.9 ns | 351.96 ns | 19.29 ns |  0.85 |    0.01 |    1 | 0.0134 | 0.0114 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,388.5 ns | 395.47 ns | 21.68 ns |  0.84 |    0.01 |    1 | 0.0134 | 0.0114 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,212.6 ns | 230.61 ns | 12.64 ns |  0.73 |    0.01 |    1 | 0.0134 | 0.0114 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,254.2 ns |  98.39 ns |  5.39 ns |  0.76 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,163.6 ns | 246.26 ns | 13.50 ns |  0.70 |    0.01 |    1 | 0.0153 | 0.0134 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            | 1,075.1 ns | 812.75 ns | 44.55 ns |  0.65 |    0.02 |    1 | 0.0153 | 0.0134 |     384 B |        0.89 |
| &#39;Get all assessments&#39;                                | 1,016.9 ns | 655.13 ns | 35.91 ns |  0.61 |    0.02 |    1 | 0.0153 | 0.0134 |     384 B |        0.89 |
