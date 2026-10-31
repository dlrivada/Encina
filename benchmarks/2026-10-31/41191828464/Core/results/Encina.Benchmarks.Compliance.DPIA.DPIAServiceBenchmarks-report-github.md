```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,458.1 ns |   899.5 ns | 49.30 ns |  1.00 |    0.04 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1,249.9 ns |   720.7 ns | 39.50 ns |  0.86 |    0.03 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   878.5 ns | 1,715.3 ns | 94.02 ns |  0.60 |    0.06 |    1 | 0.0038 | 0.0019 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;               | 1,193.6 ns |   699.0 ns | 38.31 ns |  0.82 |    0.03 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
