```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                               | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,533.9 ns | 24.89 ns | 37.25 ns | 1,530.3 ns |  1.00 |    0.03 |    6 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,402.4 ns | 23.60 ns | 35.32 ns | 1,406.0 ns |  0.91 |    0.03 |    5 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   923.6 ns | 11.30 ns | 16.91 ns |   926.2 ns |  0.60 |    0.02 |    2 | 0.0191 | 0.0181 |     336 B |        0.72 |
| &#39;Request DPO consultation&#39;                           | 1,344.1 ns | 49.20 ns | 72.11 ns | 1,384.1 ns |  0.88 |    0.05 |    5 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,361.3 ns | 38.73 ns | 56.78 ns | 2,358.7 ns |  1.54 |    0.05 |    7 | 0.0534 | 0.0267 |    1008 B |        2.17 |
| &#39;Reject assessment&#39;                                  | 1,255.1 ns | 13.89 ns | 20.79 ns | 1,254.9 ns |  0.82 |    0.02 |    5 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Request revision&#39;                                   | 1,254.6 ns | 12.46 ns | 18.66 ns | 1,256.3 ns |  0.82 |    0.02 |    5 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Expire assessment&#39;                                  | 1,100.7 ns | 10.42 ns | 14.61 ns | 1,099.6 ns |  0.72 |    0.02 |    4 | 0.0210 | 0.0191 |     384 B |        0.83 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,130.0 ns | 12.48 ns | 18.67 ns | 1,129.8 ns |  0.74 |    0.02 |    4 | 0.0248 | 0.0229 |     456 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   979.3 ns | 10.54 ns | 15.77 ns |   978.0 ns |  0.64 |    0.02 |    3 | 0.0229 | 0.0210 |     424 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   913.6 ns |  8.31 ns | 11.93 ns |   915.2 ns |  0.60 |    0.02 |    2 | 0.0229 | 0.0219 |     400 B |        0.86 |
| &#39;Get all assessments&#39;                                |   871.9 ns | 13.73 ns | 20.54 ns |   871.0 ns |  0.57 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
