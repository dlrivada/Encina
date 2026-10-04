```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      |   820.3 ns | 10.33 ns | 14.48 ns |  1.00 |    0.02 |    4 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  |   787.7 ns | 11.84 ns | 17.36 ns |  0.96 |    0.03 |    4 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   527.4 ns | 11.35 ns | 16.64 ns |  0.64 |    0.02 |    1 | 0.0191 | 0.0181 |     336 B |        0.75 |
| &#39;Request DPO consultation&#39;                           |   742.7 ns | 14.30 ns | 21.41 ns |  0.91 |    0.03 |    3 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 1,303.3 ns | 24.86 ns | 36.43 ns |  1.59 |    0.05 |    5 | 0.0553 | 0.0267 |     976 B |        2.18 |
| &#39;Reject assessment&#39;                                  |   737.8 ns | 14.69 ns | 21.07 ns |  0.90 |    0.03 |    3 | 0.0219 | 0.0210 |     384 B |        0.86 |
| &#39;Request revision&#39;                                   |   736.3 ns | 12.59 ns | 17.23 ns |  0.90 |    0.03 |    3 | 0.0219 | 0.0210 |     384 B |        0.86 |
| &#39;Expire assessment&#39;                                  |   641.8 ns | 13.43 ns | 19.69 ns |  0.78 |    0.03 |    2 | 0.0210 | 0.0200 |     368 B |        0.82 |
| &#39;Get assessment by ID (cached)&#39;                      |   648.9 ns | 26.35 ns | 39.44 ns |  0.79 |    0.05 |    2 | 0.0248 | 0.0238 |     440 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   557.7 ns |  9.35 ns | 14.00 ns |  0.68 |    0.02 |    1 | 0.0229 | 0.0219 |     408 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   512.8 ns | 15.85 ns | 23.72 ns |  0.63 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        0.89 |
| &#39;Get all assessments&#39;                                |   491.5 ns | 10.70 ns | 15.35 ns |  0.60 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.89 |
