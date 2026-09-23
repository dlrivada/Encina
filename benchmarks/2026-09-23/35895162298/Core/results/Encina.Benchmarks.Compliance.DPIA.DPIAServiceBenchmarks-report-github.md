```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,496.4 ns | 451.9 ns | 24.77 ns |  1.00 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,344.5 ns | 191.2 ns | 10.48 ns |  0.90 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   945.1 ns | 105.0 ns |  5.75 ns |  0.63 |    0.01 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           | 1,265.9 ns | 383.1 ns | 21.00 ns |  0.85 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,292.7 ns | 541.3 ns | 29.67 ns |  1.53 |    0.03 |    2 | 0.0534 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,232.3 ns | 565.4 ns | 30.99 ns |  0.82 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,211.8 ns | 165.1 ns |  9.05 ns |  0.81 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,058.9 ns | 149.2 ns |  8.18 ns |  0.71 |    0.01 |    1 | 0.0210 | 0.0191 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,149.3 ns | 602.1 ns | 33.01 ns |  0.77 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   962.0 ns | 463.5 ns | 25.41 ns |  0.64 |    0.02 |    1 | 0.0229 | 0.0210 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   905.0 ns | 344.5 ns | 18.88 ns |  0.60 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   907.8 ns | 337.6 ns | 18.50 ns |  0.61 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
