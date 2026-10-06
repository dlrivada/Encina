```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      |   936.4 ns | 150.73 ns |  8.26 ns |  1.00 |    0.01 |    1 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  |   803.6 ns | 318.18 ns | 17.44 ns |  0.86 |    0.02 |    1 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   548.7 ns | 183.89 ns | 10.08 ns |  0.59 |    0.01 |    1 | 0.0191 | 0.0181 |     336 B |        0.75 |
| &#39;Request DPO consultation&#39;                           |   760.7 ns | 258.50 ns | 14.17 ns |  0.81 |    0.01 |    1 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 1,392.3 ns | 604.35 ns | 33.13 ns |  1.49 |    0.03 |    2 | 0.0553 | 0.0267 |     944 B |        2.11 |
| &#39;Reject assessment&#39;                                  |   830.7 ns | 453.51 ns | 24.86 ns |  0.89 |    0.02 |    1 | 0.0219 | 0.0210 |     384 B |        0.86 |
| &#39;Request revision&#39;                                   |   777.6 ns | 209.81 ns | 11.50 ns |  0.83 |    0.01 |    1 | 0.0219 | 0.0210 |     384 B |        0.86 |
| &#39;Expire assessment&#39;                                  |   689.1 ns | 550.02 ns | 30.15 ns |  0.74 |    0.03 |    1 | 0.0210 | 0.0200 |     368 B |        0.82 |
| &#39;Get assessment by ID (cached)&#39;                      |   708.6 ns | 344.38 ns | 18.88 ns |  0.76 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   582.6 ns | 847.25 ns | 46.44 ns |  0.62 |    0.04 |    1 | 0.0229 | 0.0219 |     408 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   551.3 ns |  79.74 ns |  4.37 ns |  0.59 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.89 |
| &#39;Get all assessments&#39;                                |   521.2 ns | 154.37 ns |  8.46 ns |  0.56 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.89 |
