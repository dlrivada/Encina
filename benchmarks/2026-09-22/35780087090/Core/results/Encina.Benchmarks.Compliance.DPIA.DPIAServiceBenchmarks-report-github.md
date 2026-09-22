```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,185.3 ns |   141.48 ns |  7.76 ns |  1.00 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,095.0 ns |   100.33 ns |  5.50 ns |  0.92 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   746.1 ns |    94.06 ns |  5.16 ns |  0.63 |    0.01 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           | 1,001.6 ns |   445.85 ns | 24.44 ns |  0.85 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 1,812.9 ns | 1,087.10 ns | 59.59 ns |  1.53 |    0.04 |    2 | 0.0553 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  |   987.7 ns |   148.80 ns |  8.16 ns |  0.83 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   |   987.5 ns |    94.38 ns |  5.17 ns |  0.83 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  |   853.6 ns |    34.09 ns |  1.87 ns |  0.72 |    0.00 |    1 | 0.0210 | 0.0200 |     368 B |        0.85 |
| &#39;Get assessment by ID (cached)&#39;                      |   908.6 ns |    92.17 ns |  5.05 ns |  0.77 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        1.02 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   746.5 ns |    87.92 ns |  4.82 ns |  0.63 |    0.01 |    1 | 0.0229 | 0.0219 |     408 B |        0.94 |
| &#39;Get expired assessments&#39;                            |   717.9 ns |   335.44 ns | 18.39 ns |  0.61 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   709.1 ns |   778.15 ns | 42.65 ns |  0.60 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
