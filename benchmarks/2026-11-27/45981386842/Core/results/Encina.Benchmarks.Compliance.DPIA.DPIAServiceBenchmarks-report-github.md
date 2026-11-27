```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,034.2 ns |   876.9 ns | 48.06 ns |  1.00 |    0.06 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1,139.3 ns | 1,034.7 ns | 56.72 ns |  1.10 |    0.06 |    2 | 0.0048 | 0.0038 |     448 B |        1.04 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   790.5 ns |   887.1 ns | 48.62 ns |  0.77 |    0.05 |    1 | 0.0038 | 0.0029 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;               | 1,032.8 ns |   987.5 ns | 54.13 ns |  1.00 |    0.06 |    2 | 0.0048 | 0.0038 |     448 B |        1.04 |
