```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,493.1 ns | 18.91 ns | 26.50 ns |  1.00 |    0.02 |    5 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,340.6 ns | 24.68 ns | 36.18 ns |  0.90 |    0.03 |    4 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   896.6 ns |  9.24 ns | 13.54 ns |  0.60 |    0.01 |    1 | 0.0191 | 0.0181 |     336 B |        0.72 |
| &#39;Request DPO consultation&#39;                           | 1,284.3 ns | 19.86 ns | 29.12 ns |  0.86 |    0.02 |    4 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,207.5 ns | 19.70 ns | 29.49 ns |  1.48 |    0.03 |    6 | 0.0534 | 0.0267 |    1008 B |        2.17 |
| &#39;Reject assessment&#39;                                  | 1,248.1 ns | 11.52 ns | 17.24 ns |  0.84 |    0.02 |    4 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Request revision&#39;                                   | 1,218.2 ns | 10.76 ns | 16.10 ns |  0.82 |    0.02 |    4 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Expire assessment&#39;                                  | 1,068.5 ns | 13.59 ns | 19.92 ns |  0.72 |    0.02 |    3 | 0.0210 | 0.0191 |     384 B |        0.83 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,111.2 ns | 19.38 ns | 29.01 ns |  0.74 |    0.02 |    3 | 0.0248 | 0.0229 |     456 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   978.9 ns | 18.72 ns | 28.02 ns |  0.66 |    0.02 |    2 | 0.0229 | 0.0210 |     424 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   894.7 ns | 14.70 ns | 21.55 ns |  0.60 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
| &#39;Get all assessments&#39;                                |   895.6 ns | 12.18 ns | 18.24 ns |  0.60 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
