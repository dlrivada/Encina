```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,391.8 ns |   408.4 ns |  22.39 ns |  1.00 |    0.02 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,232.4 ns |   582.5 ns |  31.93 ns |  0.89 |    0.02 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   931.4 ns |   156.5 ns |   8.58 ns |  0.67 |    0.01 |    1 | 0.0038 | 0.0029 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           | 1,161.6 ns |   732.0 ns |  40.12 ns |  0.83 |    0.03 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,089.1 ns | 1,738.0 ns |  95.27 ns |  1.50 |    0.06 |    2 | 0.0076 | 0.0038 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,242.0 ns | 3,975.1 ns | 217.89 ns |  0.89 |    0.14 |    1 | 0.0038 | 0.0019 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,105.6 ns |   745.2 ns |  40.85 ns |  0.79 |    0.03 |    1 | 0.0038 | 0.0019 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,190.3 ns |   752.9 ns |  41.27 ns |  0.86 |    0.03 |    1 | 0.0038 | 0.0029 |     368 B |        0.85 |
| &#39;Get assessment by ID (cached)&#39;                      |   988.8 ns |   649.2 ns |  35.59 ns |  0.71 |    0.02 |    1 | 0.0038 | 0.0019 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   977.5 ns |   430.3 ns |  23.59 ns |  0.70 |    0.02 |    1 | 0.0038 | 0.0029 |     408 B |        0.94 |
| &#39;Get expired assessments&#39;                            |   919.9 ns |   592.6 ns |  32.48 ns |  0.66 |    0.02 |    1 | 0.0038 | 0.0029 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   900.4 ns |   315.4 ns |  17.29 ns |  0.65 |    0.01 |    1 | 0.0038 | 0.0029 |     400 B |        0.93 |
