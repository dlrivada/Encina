```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,043.1 ns |   730.3 ns | 40.03 ns |  1.00 |    0.05 |    2 | 0.0038 | 0.0019 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 1,146.7 ns |   869.9 ns | 47.68 ns |  1.10 |    0.05 |    2 | 0.0048 | 0.0038 |     448 B |        1.04 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   743.5 ns | 1,105.0 ns | 60.57 ns |  0.71 |    0.06 |    1 | 0.0038 | 0.0029 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;               | 1,092.4 ns |   836.2 ns | 45.84 ns |  1.05 |    0.05 |    2 | 0.0048 | 0.0038 |     448 B |        1.04 |
