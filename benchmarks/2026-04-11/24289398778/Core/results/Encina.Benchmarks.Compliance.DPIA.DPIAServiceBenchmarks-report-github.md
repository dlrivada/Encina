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
| &#39;Create assessment (fast path)&#39;                      | 1,555.2 ns | 16.64 ns | 24.39 ns |  1.00 |    0.02 |    6 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,353.5 ns | 23.11 ns | 33.14 ns |  0.87 |    0.02 |    5 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   935.3 ns |  9.46 ns | 13.87 ns |  0.60 |    0.01 |    2 | 0.0191 | 0.0181 |     336 B |        0.72 |
| &#39;Request DPO consultation&#39;                           | 1,289.7 ns | 15.91 ns | 23.81 ns |  0.83 |    0.02 |    4 | 0.0248 | 0.0229 |     464 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,265.4 ns | 22.65 ns | 33.21 ns |  1.46 |    0.03 |    7 | 0.0534 | 0.0267 |    1008 B |        2.17 |
| &#39;Reject assessment&#39;                                  | 1,271.8 ns | 18.55 ns | 27.77 ns |  0.82 |    0.02 |    4 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Request revision&#39;                                   | 1,247.5 ns | 14.28 ns | 20.93 ns |  0.80 |    0.02 |    4 | 0.0210 | 0.0191 |     400 B |        0.86 |
| &#39;Expire assessment&#39;                                  | 1,064.1 ns | 13.24 ns | 19.82 ns |  0.68 |    0.02 |    3 | 0.0210 | 0.0191 |     384 B |        0.83 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,068.2 ns | 15.90 ns | 23.80 ns |  0.69 |    0.02 |    3 | 0.0248 | 0.0229 |     456 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   952.0 ns |  9.71 ns | 13.93 ns |  0.61 |    0.01 |    2 | 0.0229 | 0.0210 |     424 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   867.5 ns | 12.52 ns | 18.35 ns |  0.56 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
| &#39;Get all assessments&#39;                                |   857.8 ns | 17.61 ns | 26.36 ns |  0.55 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.86 |
