```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      |   816.4 ns | 136.68 ns |  7.49 ns |  1.00 |    0.01 |    1 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  |   741.2 ns | 207.16 ns | 11.36 ns |  0.91 |    0.01 |    1 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   538.2 ns | 450.66 ns | 24.70 ns |  0.66 |    0.03 |    1 | 0.0191 | 0.0181 |     336 B |        0.75 |
| &#39;Request DPO consultation&#39;                           |   689.9 ns | 104.45 ns |  5.73 ns |  0.84 |    0.01 |    1 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 1,230.5 ns | 159.34 ns |  8.73 ns |  1.51 |    0.02 |    2 | 0.0553 | 0.0267 |     944 B |        2.11 |
| &#39;Reject assessment&#39;                                  |   687.0 ns |  25.57 ns |  1.40 ns |  0.84 |    0.01 |    1 | 0.0219 | 0.0210 |     384 B |        0.86 |
| &#39;Request revision&#39;                                   |   687.0 ns | 105.42 ns |  5.78 ns |  0.84 |    0.01 |    1 | 0.0219 | 0.0210 |     384 B |        0.86 |
| &#39;Expire assessment&#39;                                  |   585.2 ns |  68.62 ns |  3.76 ns |  0.72 |    0.01 |    1 | 0.0210 | 0.0200 |     368 B |        0.82 |
| &#39;Get assessment by ID (cached)&#39;                      |   599.8 ns | 136.60 ns |  7.49 ns |  0.73 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   546.5 ns | 515.09 ns | 28.23 ns |  0.67 |    0.03 |    1 | 0.0229 | 0.0219 |     408 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   482.6 ns | 191.10 ns | 10.47 ns |  0.59 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.89 |
| &#39;Get all assessments&#39;                                |   471.4 ns | 163.09 ns |  8.94 ns |  0.58 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.89 |
