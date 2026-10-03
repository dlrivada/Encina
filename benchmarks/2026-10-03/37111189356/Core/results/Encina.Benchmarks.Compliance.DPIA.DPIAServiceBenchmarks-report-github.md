```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,073.8 ns |   683.4 ns | 37.46 ns |  1.00 |    0.04 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  |   976.6 ns |   801.6 ns | 43.94 ns |  0.91 |    0.04 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   757.4 ns |   849.2 ns | 46.55 ns |  0.71 |    0.04 |    1 | 0.0038 | 0.0029 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           | 1,107.7 ns |   890.1 ns | 48.79 ns |  1.03 |    0.05 |    1 | 0.0048 | 0.0038 |     448 B |        1.04 |
| &#39;Approve assessment&#39;                                 | 1,836.3 ns |   614.5 ns | 33.68 ns |  1.71 |    0.06 |    2 | 0.0095 | 0.0076 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,094.5 ns | 1,436.0 ns | 78.71 ns |  1.02 |    0.07 |    1 | 0.0038 | 0.0019 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,190.8 ns |   560.1 ns | 30.70 ns |  1.11 |    0.04 |    1 | 0.0038 | 0.0029 |     384 B |        0.89 |
| &#39;Expire assessment&#39;                                  |   866.5 ns |   562.4 ns | 30.83 ns |  0.81 |    0.03 |    1 | 0.0038 | 0.0019 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      |   933.4 ns |   748.6 ns | 41.03 ns |  0.87 |    0.04 |    1 | 0.0048 | 0.0038 |     440 B |        1.02 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   803.0 ns |   227.2 ns | 12.45 ns |  0.75 |    0.02 |    1 | 0.0038 | 0.0029 |     408 B |        0.94 |
| &#39;Get expired assessments&#39;                            |   747.5 ns |   753.4 ns | 41.30 ns |  0.70 |    0.04 |    1 | 0.0038 | 0.0029 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   742.2 ns |   225.8 ns | 12.38 ns |  0.69 |    0.02 |    1 | 0.0038 | 0.0029 |     400 B |        0.93 |
