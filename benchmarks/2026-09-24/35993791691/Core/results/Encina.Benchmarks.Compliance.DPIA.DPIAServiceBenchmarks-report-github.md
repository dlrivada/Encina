```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,480.5 ns |  50.93 ns |  2.79 ns |  1.00 |    0.00 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,299.7 ns | 169.77 ns |  9.31 ns |  0.88 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   922.9 ns |  49.30 ns |  2.70 ns |  0.62 |    0.00 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           | 1,235.2 ns | 117.59 ns |  6.45 ns |  0.83 |    0.00 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,221.6 ns |  38.81 ns |  2.13 ns |  1.50 |    0.00 |    2 | 0.0534 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,227.1 ns | 242.38 ns | 13.29 ns |  0.83 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,208.1 ns | 187.19 ns | 10.26 ns |  0.82 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,069.1 ns | 361.25 ns | 19.80 ns |  0.72 |    0.01 |    1 | 0.0210 | 0.0191 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,097.6 ns | 547.62 ns | 30.02 ns |  0.74 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   931.3 ns |  68.00 ns |  3.73 ns |  0.63 |    0.00 |    1 | 0.0229 | 0.0210 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   861.3 ns | 171.26 ns |  9.39 ns |  0.58 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   834.8 ns | 216.07 ns | 11.84 ns |  0.56 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
