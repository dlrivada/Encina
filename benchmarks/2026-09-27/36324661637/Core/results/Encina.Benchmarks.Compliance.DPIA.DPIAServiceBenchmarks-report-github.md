```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,183.2 ns | 490.73 ns | 26.90 ns |  1.00 |    0.03 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,061.4 ns | 137.40 ns |  7.53 ns |  0.90 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   757.4 ns | 107.68 ns |  5.90 ns |  0.64 |    0.01 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           | 1,032.9 ns |  27.92 ns |  1.53 ns |  0.87 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 1,756.5 ns |  39.56 ns |  2.17 ns |  1.49 |    0.03 |    2 | 0.0553 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  |   976.8 ns |  59.72 ns |  3.27 ns |  0.83 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   |   988.5 ns | 390.18 ns | 21.39 ns |  0.84 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  |   844.2 ns | 105.47 ns |  5.78 ns |  0.71 |    0.01 |    1 | 0.0210 | 0.0200 |     368 B |        0.85 |
| &#39;Get assessment by ID (cached)&#39;                      |   879.7 ns | 130.90 ns |  7.18 ns |  0.74 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.02 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   756.9 ns |  46.22 ns |  2.53 ns |  0.64 |    0.01 |    1 | 0.0229 | 0.0219 |     408 B |        0.94 |
| &#39;Get expired assessments&#39;                            |   718.7 ns | 105.70 ns |  5.79 ns |  0.61 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   695.8 ns | 146.25 ns |  8.02 ns |  0.59 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
