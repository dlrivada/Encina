```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      |   823.4 ns |    89.51 ns |   4.91 ns |  1.00 |    0.01 |    1 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  |   774.5 ns |   788.29 ns |  43.21 ns |  0.94 |    0.05 |    1 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   511.6 ns |   596.97 ns |  32.72 ns |  0.62 |    0.03 |    1 | 0.0191 | 0.0181 |     336 B |        0.75 |
| &#39;Request DPO consultation&#39;                           |   718.0 ns |    93.81 ns |   5.14 ns |  0.87 |    0.01 |    1 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 1,362.0 ns | 2,999.37 ns | 164.41 ns |  1.65 |    0.17 |    2 | 0.0553 | 0.0267 |     944 B |        2.11 |
| &#39;Reject assessment&#39;                                  |   695.0 ns |   295.61 ns |  16.20 ns |  0.84 |    0.02 |    1 | 0.0219 | 0.0210 |     384 B |        0.86 |
| &#39;Request revision&#39;                                   |   698.0 ns |   326.32 ns |  17.89 ns |  0.85 |    0.02 |    1 | 0.0219 | 0.0210 |     384 B |        0.86 |
| &#39;Expire assessment&#39;                                  |   594.6 ns |   123.56 ns |   6.77 ns |  0.72 |    0.01 |    1 | 0.0210 | 0.0200 |     368 B |        0.82 |
| &#39;Get assessment by ID (cached)&#39;                      |   630.3 ns |   183.22 ns |  10.04 ns |  0.77 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   535.2 ns |   166.15 ns |   9.11 ns |  0.65 |    0.01 |    1 | 0.0229 | 0.0219 |     408 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   481.8 ns |   157.40 ns |   8.63 ns |  0.59 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.89 |
| &#39;Get all assessments&#39;                                |   478.4 ns |    26.88 ns |   1.47 ns |  0.58 |    0.00 |    1 | 0.0229 | 0.0219 |     400 B |        0.89 |
