```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,453.6 ns |   833.9 ns | 45.71 ns |  1.00 |    0.04 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1,299.6 ns |   494.4 ns | 27.10 ns |  0.89 |    0.03 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   885.0 ns | 1,215.3 ns | 66.61 ns |  0.61 |    0.04 |    1 | 0.0038 | 0.0019 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;               | 1,170.1 ns | 1,536.4 ns | 84.22 ns |  0.81 |    0.05 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
