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
| &#39;Create assessment (fast path)&#39;                      | 1,470.0 ns | 252.11 ns | 13.82 ns |  1.00 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,489.1 ns | 277.38 ns | 15.20 ns |  1.01 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   920.6 ns | 297.60 ns | 16.31 ns |  0.63 |    0.01 |    1 | 0.0191 | 0.0172 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;                           | 1,296.7 ns | 237.73 ns | 13.03 ns |  0.88 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,317.3 ns | 905.72 ns | 49.65 ns |  1.58 |    0.03 |    2 | 0.0534 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,278.2 ns | 245.44 ns | 13.45 ns |  0.87 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,285.7 ns | 257.71 ns | 14.13 ns |  0.87 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,094.3 ns | 409.76 ns | 22.46 ns |  0.74 |    0.01 |    1 | 0.0210 | 0.0191 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,167.9 ns | 475.27 ns | 26.05 ns |  0.79 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   995.2 ns |  49.74 ns |  2.73 ns |  0.68 |    0.01 |    1 | 0.0229 | 0.0210 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   920.8 ns | 270.21 ns | 14.81 ns |  0.63 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   912.3 ns | 263.39 ns | 14.44 ns |  0.62 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
