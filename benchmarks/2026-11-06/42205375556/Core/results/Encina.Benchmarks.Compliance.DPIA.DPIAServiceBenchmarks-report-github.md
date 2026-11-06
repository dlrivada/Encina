```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,179.9 ns | 548.8 ns | 30.08 ns |  1.00 |    0.03 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1,082.3 ns | 726.6 ns | 39.83 ns |  0.92 |    0.04 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   878.1 ns | 833.6 ns | 45.69 ns |  0.74 |    0.04 |    1 | 0.0038 | 0.0029 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;               | 1,003.5 ns | 396.9 ns | 21.76 ns |  0.85 |    0.02 |    1 | 0.0038 | 0.0019 |     432 B |        1.00 |
