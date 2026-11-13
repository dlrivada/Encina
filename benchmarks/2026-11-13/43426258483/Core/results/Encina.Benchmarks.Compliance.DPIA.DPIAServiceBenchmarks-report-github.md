```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1.719 μs | 0.4882 μs | 0.0268 μs |  1.00 |    0.02 |    2 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1.539 μs | 0.0365 μs | 0.0020 μs |  0.90 |    0.01 |    2 | 0.0172 | 0.0153 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; | 1.040 μs | 0.2222 μs | 0.0122 μs |  0.61 |    0.01 |    1 | 0.0114 | 0.0095 |     320 B |        0.74 |
| &#39;Request DPO consultation&#39;               | 1.482 μs | 0.0410 μs | 0.0022 μs |  0.86 |    0.01 |    2 | 0.0172 | 0.0153 |     432 B |        1.00 |
