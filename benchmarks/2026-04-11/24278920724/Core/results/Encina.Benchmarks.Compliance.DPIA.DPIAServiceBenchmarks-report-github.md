```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,429.5 ns |  6.00 ns |  8.80 ns |  1.00 |    0.01 |    5 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,279.9 ns |  7.38 ns | 11.04 ns |  0.90 |    0.01 |    4 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   862.4 ns |  3.98 ns |  5.84 ns |  0.60 |    0.01 |    1 | 0.0191 | 0.0181 |     336 B |        0.72 |
| &#39;Request DPO consultation&#39;                           | 1,206.8 ns | 15.57 ns | 23.31 ns |  0.84 |    0.02 |    4 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,202.5 ns | 13.51 ns | 20.22 ns |  1.54 |    0.02 |    6 | 0.0534 | 0.0267 |    1008 B |        2.17 |
| &#39;Reject assessment&#39;                                  | 1,239.6 ns | 14.04 ns | 20.57 ns |  0.87 |    0.02 |    4 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Request revision&#39;                                   | 1,204.4 ns | 14.44 ns | 21.62 ns |  0.84 |    0.02 |    4 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Expire assessment&#39;                                  | 1,027.0 ns |  7.80 ns | 10.94 ns |  0.72 |    0.01 |    3 | 0.0210 | 0.0191 |     384 B |        0.83 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,037.8 ns | 11.76 ns | 17.60 ns |  0.73 |    0.01 |    3 | 0.0248 | 0.0229 |     456 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   934.6 ns |  4.02 ns |  6.01 ns |  0.65 |    0.01 |    2 | 0.0229 | 0.0219 |     408 B |        0.88 |
| &#39;Get expired assessments&#39;                            |   842.4 ns |  5.80 ns |  8.68 ns |  0.59 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
| &#39;Get all assessments&#39;                                |   818.1 ns |  7.34 ns | 10.75 ns |  0.57 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
