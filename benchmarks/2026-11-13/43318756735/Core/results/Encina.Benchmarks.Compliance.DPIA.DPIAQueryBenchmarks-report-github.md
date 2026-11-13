```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 857.8 ns | 275.7 ns | 15.11 ns |  1.00 |    0.02 |    1 | 0.0248 | 0.0238 |     440 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 759.3 ns | 129.1 ns |  7.08 ns |  0.89 |    0.02 |    1 | 0.0229 | 0.0219 |     408 B |        0.93 |
| &#39;Get expired assessments&#39;                            | 686.3 ns | 196.1 ns | 10.75 ns |  0.80 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.91 |
| &#39;Get all assessments&#39;                                | 680.8 ns | 214.0 ns | 11.73 ns |  0.79 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.91 |
