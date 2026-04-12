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
| &#39;Create assessment (fast path)&#39;                      | 1,524.9 ns | 52.47 ns | 76.91 ns | 1,572.4 ns |  1.00 |    0.07 |    6 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,289.8 ns | 11.57 ns | 16.97 ns | 1,291.0 ns |  0.85 |    0.04 |    5 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   883.4 ns | 12.93 ns | 18.95 ns |   880.2 ns |  0.58 |    0.03 |    2 | 0.0191 | 0.0181 |     336 B |        0.72 |
| &#39;Request DPO consultation&#39;                           | 1,242.6 ns | 12.30 ns | 18.42 ns | 1,241.4 ns |  0.82 |    0.04 |    5 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,194.8 ns | 17.16 ns | 24.06 ns | 2,204.2 ns |  1.44 |    0.07 |    7 | 0.0534 | 0.0267 |    1008 B |        2.17 |
| &#39;Reject assessment&#39;                                  | 1,245.4 ns | 21.08 ns | 31.55 ns | 1,240.7 ns |  0.82 |    0.05 |    5 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Request revision&#39;                                   | 1,230.1 ns | 19.26 ns | 28.83 ns | 1,235.4 ns |  0.81 |    0.04 |    5 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Expire assessment&#39;                                  | 1,079.1 ns | 16.27 ns | 23.34 ns | 1,078.2 ns |  0.71 |    0.04 |    4 | 0.0210 | 0.0191 |     384 B |        0.83 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,087.8 ns | 16.60 ns | 24.84 ns | 1,086.8 ns |  0.72 |    0.04 |    4 | 0.0248 | 0.0229 |     456 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   946.1 ns | 10.21 ns | 15.28 ns |   944.9 ns |  0.62 |    0.03 |    3 | 0.0229 | 0.0210 |     424 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   883.9 ns |  7.55 ns | 11.07 ns |   885.5 ns |  0.58 |    0.03 |    2 | 0.0229 | 0.0219 |     400 B |        0.86 |
| &#39;Get all assessments&#39;                                |   836.2 ns | 10.25 ns | 15.02 ns |   834.6 ns |  0.55 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
