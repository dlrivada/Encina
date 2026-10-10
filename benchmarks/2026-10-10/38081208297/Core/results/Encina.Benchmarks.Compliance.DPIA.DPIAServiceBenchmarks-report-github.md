```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,568.4 ns | 402.28 ns | 22.05 ns |  1.00 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,384.4 ns | 492.86 ns | 27.02 ns |  0.88 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   952.3 ns | 697.45 ns | 38.23 ns |  0.61 |    0.02 |    1 | 0.0191 | 0.0172 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;                           | 1,309.0 ns | 101.92 ns |  5.59 ns |  0.83 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,431.7 ns | 586.25 ns | 32.13 ns |  1.55 |    0.03 |    2 | 0.0534 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,274.7 ns |  77.91 ns |  4.27 ns |  0.81 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,240.1 ns | 165.82 ns |  9.09 ns |  0.79 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,073.8 ns | 331.62 ns | 18.18 ns |  0.68 |    0.01 |    1 | 0.0210 | 0.0191 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,144.9 ns | 323.93 ns | 17.76 ns |  0.73 |    0.01 |    1 | 0.0248 | 0.0229 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,001.1 ns | 365.35 ns | 20.03 ns |  0.64 |    0.01 |    1 | 0.0229 | 0.0210 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   880.9 ns | 609.17 ns | 33.39 ns |  0.56 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   888.1 ns | 452.41 ns | 24.80 ns |  0.57 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
