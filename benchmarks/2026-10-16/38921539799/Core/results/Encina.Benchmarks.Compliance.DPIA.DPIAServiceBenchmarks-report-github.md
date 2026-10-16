```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      |   861.3 ns |   367.69 ns | 20.15 ns |  1.00 |    0.03 |    1 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  |   746.8 ns |   249.24 ns | 13.66 ns |  0.87 |    0.02 |    1 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   507.7 ns |    72.09 ns |  3.95 ns |  0.59 |    0.01 |    1 | 0.0191 | 0.0181 |     336 B |        0.75 |
| &#39;Request DPO consultation&#39;                           |   718.7 ns |   234.46 ns | 12.85 ns |  0.83 |    0.02 |    1 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 1,299.0 ns | 1,016.04 ns | 55.69 ns |  1.51 |    0.06 |    2 | 0.0553 | 0.0267 |     944 B |        2.11 |
| &#39;Reject assessment&#39;                                  |   705.2 ns |   264.74 ns | 14.51 ns |  0.82 |    0.02 |    1 | 0.0219 | 0.0210 |     384 B |        0.86 |
| &#39;Request revision&#39;                                   |   692.0 ns |   185.73 ns | 10.18 ns |  0.80 |    0.02 |    1 | 0.0219 | 0.0210 |     384 B |        0.86 |
| &#39;Expire assessment&#39;                                  |   609.8 ns |   120.00 ns |  6.58 ns |  0.71 |    0.02 |    1 | 0.0210 | 0.0200 |     368 B |        0.82 |
| &#39;Get assessment by ID (cached)&#39;                      |   641.0 ns |   784.17 ns | 42.98 ns |  0.74 |    0.05 |    1 | 0.0248 | 0.0238 |     440 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   549.8 ns |   301.34 ns | 16.52 ns |  0.64 |    0.02 |    1 | 0.0229 | 0.0219 |     408 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   524.3 ns |   734.79 ns | 40.28 ns |  0.61 |    0.04 |    1 | 0.0229 | 0.0219 |     400 B |        0.89 |
| &#39;Get all assessments&#39;                                |   491.0 ns |   228.59 ns | 12.53 ns |  0.57 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.89 |
