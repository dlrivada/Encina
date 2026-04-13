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
| &#39;Create assessment (fast path)&#39;                      | 1,492.4 ns | 13.77 ns | 20.61 ns |  1.00 |    0.02 |    5 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,334.7 ns | 20.55 ns | 30.76 ns |  0.89 |    0.02 |    4 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   886.1 ns |  7.32 ns | 10.73 ns |  0.59 |    0.01 |    1 | 0.0191 | 0.0181 |     336 B |        0.72 |
| &#39;Request DPO consultation&#39;                           | 1,297.8 ns | 13.76 ns | 20.60 ns |  0.87 |    0.02 |    4 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,266.9 ns | 37.72 ns | 56.46 ns |  1.52 |    0.04 |    6 | 0.0534 | 0.0267 |    1008 B |        2.17 |
| &#39;Reject assessment&#39;                                  | 1,253.8 ns | 14.26 ns | 20.90 ns |  0.84 |    0.02 |    4 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Request revision&#39;                                   | 1,216.8 ns | 13.75 ns | 20.58 ns |  0.82 |    0.02 |    4 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Expire assessment&#39;                                  | 1,077.2 ns | 11.32 ns | 16.60 ns |  0.72 |    0.01 |    3 | 0.0210 | 0.0191 |     384 B |        0.83 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,074.4 ns | 13.20 ns | 19.76 ns |  0.72 |    0.02 |    3 | 0.0248 | 0.0229 |     456 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   957.0 ns | 10.42 ns | 15.27 ns |  0.64 |    0.01 |    2 | 0.0229 | 0.0210 |     424 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   910.4 ns | 13.00 ns | 19.45 ns |  0.61 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
| &#39;Get all assessments&#39;                                |   854.9 ns |  9.36 ns | 13.13 ns |  0.57 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
