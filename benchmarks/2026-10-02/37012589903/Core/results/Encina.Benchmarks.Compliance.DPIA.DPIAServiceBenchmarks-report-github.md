```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,200.3 ns | 1,243.55 ns | 68.16 ns |  1.00 |    0.07 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,098.5 ns |   325.12 ns | 17.82 ns |  0.92 |    0.05 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   741.8 ns |    70.30 ns |  3.85 ns |  0.62 |    0.03 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           |   996.9 ns |   565.58 ns | 31.00 ns |  0.83 |    0.05 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 1,799.3 ns |   200.65 ns | 11.00 ns |  1.50 |    0.07 |    2 | 0.0553 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,015.4 ns |   488.28 ns | 26.76 ns |  0.85 |    0.04 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   |   948.6 ns |   102.31 ns |  5.61 ns |  0.79 |    0.04 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  |   845.9 ns |   131.49 ns |  7.21 ns |  0.71 |    0.03 |    1 | 0.0210 | 0.0200 |     368 B |        0.85 |
| &#39;Get assessment by ID (cached)&#39;                      |   903.4 ns |   233.73 ns | 12.81 ns |  0.75 |    0.04 |    1 | 0.0248 | 0.0238 |     440 B |        1.02 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   773.7 ns |   115.12 ns |  6.31 ns |  0.65 |    0.03 |    1 | 0.0229 | 0.0219 |     408 B |        0.94 |
| &#39;Get expired assessments&#39;                            |   724.2 ns |   203.54 ns | 11.16 ns |  0.60 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   678.0 ns |   231.75 ns | 12.70 ns |  0.57 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
