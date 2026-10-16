```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,634.3 ns |  64.86 ns |  3.56 ns |  1.00 |    0.00 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,523.5 ns | 148.32 ns |  8.13 ns |  0.93 |    0.00 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   992.3 ns |  84.46 ns |  4.63 ns |  0.61 |    0.00 |    1 | 0.0114 | 0.0095 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;                           | 1,415.5 ns | 397.40 ns | 21.78 ns |  0.87 |    0.01 |    1 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,399.8 ns | 338.47 ns | 18.55 ns |  1.47 |    0.01 |    2 | 0.0343 | 0.0305 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,355.3 ns | 697.87 ns | 38.25 ns |  0.83 |    0.02 |    1 | 0.0134 | 0.0114 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,341.4 ns | 634.12 ns | 34.76 ns |  0.82 |    0.02 |    1 | 0.0134 | 0.0114 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,155.3 ns | 247.52 ns | 13.57 ns |  0.71 |    0.01 |    1 | 0.0134 | 0.0114 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,277.3 ns | 208.04 ns | 11.40 ns |  0.78 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,159.6 ns | 166.50 ns |  9.13 ns |  0.71 |    0.01 |    1 | 0.0153 | 0.0134 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            | 1,075.2 ns | 767.29 ns | 42.06 ns |  0.66 |    0.02 |    1 | 0.0153 | 0.0134 |     384 B |        0.89 |
| &#39;Get all assessments&#39;                                | 1,066.1 ns | 540.79 ns | 29.64 ns |  0.65 |    0.02 |    1 | 0.0153 | 0.0134 |     384 B |        0.89 |
