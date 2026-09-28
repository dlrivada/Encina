```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,530.6 ns |  55.41 ns |  3.04 ns |  1.00 |    0.00 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,400.5 ns | 162.29 ns |  8.90 ns |  0.92 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   973.4 ns | 196.87 ns | 10.79 ns |  0.64 |    0.01 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           | 1,331.2 ns | 620.54 ns | 34.01 ns |  0.87 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,508.3 ns | 343.18 ns | 18.81 ns |  1.64 |    0.01 |    2 | 0.0534 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,252.1 ns | 464.48 ns | 25.46 ns |  0.82 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,261.8 ns | 254.88 ns | 13.97 ns |  0.82 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,167.9 ns | 525.17 ns | 28.79 ns |  0.76 |    0.02 |    1 | 0.0210 | 0.0191 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,198.0 ns | 506.71 ns | 27.77 ns |  0.78 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,035.2 ns | 418.28 ns | 22.93 ns |  0.68 |    0.01 |    1 | 0.0229 | 0.0210 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   967.9 ns | 482.15 ns | 26.43 ns |  0.63 |    0.01 |    1 | 0.0229 | 0.0210 |     384 B |        0.89 |
| &#39;Get all assessments&#39;                                |   917.0 ns | 253.02 ns | 13.87 ns |  0.60 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
