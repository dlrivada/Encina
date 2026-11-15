```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,430.4 ns |   988.5 ns |  54.18 ns |  1.00 |    0.05 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1,208.1 ns | 1,935.1 ns | 106.07 ns |  0.85 |    0.07 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   909.5 ns | 1,418.0 ns |  77.73 ns |  0.64 |    0.05 |    1 | 0.0038 | 0.0019 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;               | 1,206.2 ns |   536.0 ns |  29.38 ns |  0.84 |    0.03 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
