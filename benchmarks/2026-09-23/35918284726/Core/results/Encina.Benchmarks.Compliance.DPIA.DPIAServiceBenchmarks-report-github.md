```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,552.2 ns |   364.7 ns | 19.99 ns |  1.00 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,416.9 ns |   110.4 ns |  6.05 ns |  0.91 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   952.1 ns |   147.7 ns |  8.10 ns |  0.61 |    0.01 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           | 1,288.9 ns |   201.9 ns | 11.07 ns |  0.83 |    0.01 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,278.9 ns | 1,659.9 ns | 90.98 ns |  1.47 |    0.05 |    2 | 0.0534 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,234.4 ns |   295.5 ns | 16.20 ns |  0.80 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,305.4 ns |   207.8 ns | 11.39 ns |  0.84 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,108.2 ns |   411.7 ns | 22.57 ns |  0.71 |    0.01 |    1 | 0.0210 | 0.0191 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,091.3 ns |   613.5 ns | 33.63 ns |  0.70 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   984.8 ns |   218.9 ns | 12.00 ns |  0.63 |    0.01 |    1 | 0.0229 | 0.0210 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   853.6 ns |   144.7 ns |  7.93 ns |  0.55 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   823.5 ns |   190.6 ns | 10.45 ns |  0.53 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
