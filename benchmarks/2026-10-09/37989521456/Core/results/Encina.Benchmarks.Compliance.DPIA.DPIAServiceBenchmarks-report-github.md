```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,206.6 ns | 1,410.21 ns | 77.30 ns |  1.00 |    0.08 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,034.5 ns |   305.34 ns | 16.74 ns |  0.86 |    0.05 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   714.6 ns |   337.04 ns | 18.47 ns |  0.59 |    0.03 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           | 1,008.0 ns |    83.63 ns |  4.58 ns |  0.84 |    0.04 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 1,746.9 ns |   327.52 ns | 17.95 ns |  1.45 |    0.08 |    2 | 0.0553 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  |   964.0 ns |   230.74 ns | 12.65 ns |  0.80 |    0.04 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   |   931.7 ns |   116.37 ns |  6.38 ns |  0.77 |    0.04 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  |   839.0 ns |    59.81 ns |  3.28 ns |  0.70 |    0.04 |    1 | 0.0210 | 0.0200 |     368 B |        0.85 |
| &#39;Get assessment by ID (cached)&#39;                      |   895.4 ns |   477.05 ns | 26.15 ns |  0.74 |    0.04 |    1 | 0.0248 | 0.0238 |     440 B |        1.02 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   755.4 ns |   252.12 ns | 13.82 ns |  0.63 |    0.04 |    1 | 0.0229 | 0.0219 |     408 B |        0.94 |
| &#39;Get expired assessments&#39;                            |   703.3 ns |   192.58 ns | 10.56 ns |  0.58 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   679.0 ns |   182.69 ns | 10.01 ns |  0.56 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
