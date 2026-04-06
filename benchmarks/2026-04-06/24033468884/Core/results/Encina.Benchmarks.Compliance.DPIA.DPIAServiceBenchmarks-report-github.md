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
| &#39;Create assessment (fast path)&#39;                      | 1,510.9 ns | 15.79 ns | 23.63 ns |  1.00 |    0.02 |    6 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,382.7 ns | 17.66 ns | 26.43 ns |  0.92 |    0.02 |    5 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   943.6 ns |  9.78 ns | 14.34 ns |  0.62 |    0.01 |    1 | 0.0191 | 0.0172 |     352 B |        0.76 |
| &#39;Request DPO consultation&#39;                           | 1,356.6 ns | 12.05 ns | 18.04 ns |  0.90 |    0.02 |    5 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,480.6 ns | 35.84 ns | 52.53 ns |  1.64 |    0.04 |    7 | 0.0534 | 0.0267 |    1008 B |        2.17 |
| &#39;Reject assessment&#39;                                  | 1,312.9 ns | 15.89 ns | 23.78 ns |  0.87 |    0.02 |    5 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Request revision&#39;                                   | 1,270.0 ns | 11.12 ns | 16.64 ns |  0.84 |    0.02 |    5 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Expire assessment&#39;                                  | 1,119.8 ns | 12.20 ns | 18.26 ns |  0.74 |    0.02 |    3 | 0.0210 | 0.0191 |     384 B |        0.83 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,171.5 ns | 12.66 ns | 18.95 ns |  0.78 |    0.02 |    4 | 0.0248 | 0.0229 |     456 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,005.3 ns | 11.59 ns | 17.35 ns |  0.67 |    0.02 |    2 | 0.0229 | 0.0210 |     424 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   931.7 ns | 14.55 ns | 21.77 ns |  0.62 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
| &#39;Get all assessments&#39;                                |   941.5 ns | 10.34 ns | 15.47 ns |  0.62 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
