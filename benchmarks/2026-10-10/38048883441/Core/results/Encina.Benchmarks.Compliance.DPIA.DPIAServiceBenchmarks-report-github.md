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
| &#39;Create assessment (fast path)&#39;                      | 1,573.0 ns | 164.89 ns |  9.04 ns |  1.00 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,359.7 ns | 314.66 ns | 17.25 ns |  0.86 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   987.8 ns | 161.21 ns |  8.84 ns |  0.63 |    0.01 |    1 | 0.0191 | 0.0172 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;                           | 1,369.0 ns | 229.76 ns | 12.59 ns |  0.87 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,677.5 ns | 791.31 ns | 43.37 ns |  1.70 |    0.03 |    2 | 0.0534 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,250.3 ns | 326.47 ns | 17.90 ns |  0.79 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,241.6 ns | 326.93 ns | 17.92 ns |  0.79 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,098.5 ns | 407.39 ns | 22.33 ns |  0.70 |    0.01 |    1 | 0.0210 | 0.0191 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,172.8 ns | 425.65 ns | 23.33 ns |  0.75 |    0.01 |    1 | 0.0248 | 0.0229 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,002.7 ns |  85.83 ns |  4.70 ns |  0.64 |    0.00 |    1 | 0.0229 | 0.0210 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   950.0 ns | 585.52 ns | 32.09 ns |  0.60 |    0.02 |    1 | 0.0229 | 0.0210 |     384 B |        0.89 |
| &#39;Get all assessments&#39;                                |   917.2 ns | 353.12 ns | 19.36 ns |  0.58 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
