```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                   | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 955.3 ns | 23.86 ns | 33.45 ns |  1.00 |    0.05 |    3 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 862.6 ns | 35.50 ns | 52.03 ns |  0.90 |    0.06 |    2 | 0.0257 | 0.0248 |     448 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; | 584.6 ns | 25.38 ns | 37.21 ns |  0.61 |    0.04 |    1 | 0.0191 | 0.0181 |     336 B |        0.75 |
| &#39;Request DPO consultation&#39;               | 823.5 ns | 46.55 ns | 69.67 ns |  0.86 |    0.08 |    2 | 0.0257 | 0.0248 |     448 B |        1.00 |
