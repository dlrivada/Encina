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
| &#39;Create assessment (fast path)&#39;                      | 1,175.6 ns |   384.19 ns | 21.06 ns |  1.00 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,088.9 ns | 1,563.55 ns | 85.70 ns |  0.93 |    0.06 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   700.8 ns |    48.69 ns |  2.67 ns |  0.60 |    0.01 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           |   990.3 ns |   246.79 ns | 13.53 ns |  0.84 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 1,772.4 ns |   308.39 ns | 16.90 ns |  1.51 |    0.03 |    2 | 0.0553 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  |   978.0 ns |   129.38 ns |  7.09 ns |  0.83 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   |   992.0 ns |   175.40 ns |  9.61 ns |  0.84 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  |   852.3 ns |   475.48 ns | 26.06 ns |  0.73 |    0.02 |    1 | 0.0210 | 0.0200 |     368 B |        0.85 |
| &#39;Get assessment by ID (cached)&#39;                      |   923.5 ns |   282.72 ns | 15.50 ns |  0.79 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.02 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   793.4 ns |   230.37 ns | 12.63 ns |  0.68 |    0.01 |    1 | 0.0229 | 0.0219 |     408 B |        0.94 |
| &#39;Get expired assessments&#39;                            |   691.3 ns |   183.88 ns | 10.08 ns |  0.59 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   708.9 ns |   449.69 ns | 24.65 ns |  0.60 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
