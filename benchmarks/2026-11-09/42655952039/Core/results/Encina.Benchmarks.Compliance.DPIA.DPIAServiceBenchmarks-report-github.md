```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 3.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                   | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;          | 1,011.7 ns | 839.30 ns | 46.00 ns |  1.00 |    0.06 |    2 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;      |   977.2 ns | 386.92 ns | 21.21 ns |  0.97 |    0.04 |    2 | 0.0257 | 0.0248 |     448 B |        1.04 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39; |   658.1 ns | 118.45 ns |  6.49 ns |  0.65 |    0.03 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;               |   911.8 ns |  56.39 ns |  3.09 ns |  0.90 |    0.04 |    2 | 0.0257 | 0.0248 |     448 B |        1.04 |
