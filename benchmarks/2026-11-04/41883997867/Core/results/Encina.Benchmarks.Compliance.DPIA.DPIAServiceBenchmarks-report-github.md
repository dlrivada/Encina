```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean     | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |---------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 999.3 ns | 1,162.61 ns | 63.73 ns |  1.00 |    0.08 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 897.7 ns |    62.05 ns |  3.40 ns |  0.90 |    0.05 |    2 | 0.0257 | 0.0248 |     448 B |        1.04 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; | 633.2 ns |   473.28 ns | 25.94 ns |  0.64 |    0.04 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;               | 879.6 ns |   146.32 ns |  8.02 ns |  0.88 |    0.05 |    2 | 0.0257 | 0.0248 |     448 B |        1.04 |
