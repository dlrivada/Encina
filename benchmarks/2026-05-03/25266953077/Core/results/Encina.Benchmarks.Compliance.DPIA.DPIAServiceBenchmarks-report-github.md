```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,501.6 ns | 30.73 ns | 46.00 ns |  1.00 |    0.04 |    5 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,341.4 ns | 19.20 ns | 28.14 ns |  0.89 |    0.03 |    4 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   893.5 ns | 22.59 ns | 31.67 ns |  0.60 |    0.03 |    1 | 0.0191 | 0.0181 |     336 B |        0.72 |
| &#39;Request DPO consultation&#39;                           | 1,274.0 ns | 22.68 ns | 33.24 ns |  0.85 |    0.03 |    3 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,316.8 ns | 39.29 ns | 58.81 ns |  1.54 |    0.06 |    6 | 0.0534 | 0.0267 |    1008 B |        2.17 |
| &#39;Reject assessment&#39;                                  | 1,250.1 ns | 12.67 ns | 18.97 ns |  0.83 |    0.03 |    3 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Request revision&#39;                                   | 1,220.0 ns |  9.37 ns | 13.73 ns |  0.81 |    0.03 |    3 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Expire assessment&#39;                                  | 1,100.7 ns | 13.75 ns | 20.59 ns |  0.73 |    0.03 |    2 | 0.0210 | 0.0191 |     384 B |        0.83 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,143.4 ns | 20.56 ns | 30.13 ns |  0.76 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   945.6 ns | 19.61 ns | 29.36 ns |  0.63 |    0.03 |    1 | 0.0229 | 0.0210 |     424 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   844.7 ns |  7.23 ns | 10.60 ns |  0.56 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
| &#39;Get all assessments&#39;                                |   838.7 ns | 14.50 ns | 21.26 ns |  0.56 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
