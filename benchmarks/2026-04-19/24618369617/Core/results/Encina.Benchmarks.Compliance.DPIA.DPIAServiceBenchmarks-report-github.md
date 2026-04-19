```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                               | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,431.0 ns | 26.56 ns | 38.93 ns | 1,411.9 ns |  1.00 |    0.04 |    7 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,259.7 ns |  7.12 ns | 10.44 ns | 1,259.5 ns |  0.88 |    0.02 |    6 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   872.3 ns | 10.71 ns | 15.70 ns |   881.1 ns |  0.61 |    0.02 |    2 | 0.0191 | 0.0181 |     336 B |        0.72 |
| &#39;Request DPO consultation&#39;                           | 1,195.7 ns | 10.95 ns | 15.70 ns | 1,197.0 ns |  0.84 |    0.02 |    5 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,156.7 ns | 25.65 ns | 38.39 ns | 2,150.7 ns |  1.51 |    0.05 |    8 | 0.0534 | 0.0267 |    1008 B |        2.17 |
| &#39;Reject assessment&#39;                                  | 1,207.7 ns |  9.83 ns | 14.71 ns | 1,206.5 ns |  0.84 |    0.02 |    5 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Request revision&#39;                                   | 1,194.8 ns | 12.52 ns | 18.73 ns | 1,193.8 ns |  0.84 |    0.03 |    5 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Expire assessment&#39;                                  | 1,028.0 ns |  9.54 ns | 14.28 ns | 1,027.1 ns |  0.72 |    0.02 |    4 | 0.0210 | 0.0191 |     384 B |        0.83 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,028.1 ns | 13.90 ns | 20.80 ns | 1,026.2 ns |  0.72 |    0.02 |    4 | 0.0248 | 0.0229 |     456 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   908.5 ns |  2.73 ns |  4.08 ns |   908.8 ns |  0.64 |    0.02 |    3 | 0.0229 | 0.0219 |     408 B |        0.88 |
| &#39;Get expired assessments&#39;                            |   859.8 ns | 21.97 ns | 32.20 ns |   872.9 ns |  0.60 |    0.03 |    2 | 0.0229 | 0.0219 |     400 B |        0.86 |
| &#39;Get all assessments&#39;                                |   798.9 ns |  8.24 ns | 12.33 ns |   799.1 ns |  0.56 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
