```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 959.9 ns | 565.9 ns | 31.02 ns |  1.00 |    0.04 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      | 875.6 ns | 159.8 ns |  8.76 ns |  0.91 |    0.03 |    2 | 0.0257 | 0.0248 |     448 B |        1.04 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; | 620.3 ns | 298.9 ns | 16.38 ns |  0.65 |    0.02 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;               | 858.8 ns | 354.9 ns | 19.46 ns |  0.90 |    0.03 |    2 | 0.0257 | 0.0248 |     448 B |        1.04 |
