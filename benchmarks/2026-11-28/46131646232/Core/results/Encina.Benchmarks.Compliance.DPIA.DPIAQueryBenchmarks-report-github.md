```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 956.5 ns | 312.1 ns | 17.11 ns |  1.00 |    0.02 |    1 | 0.0048 | 0.0038 |     440 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 837.0 ns | 356.6 ns | 19.55 ns |  0.88 |    0.02 |    1 | 0.0038 | 0.0029 |     408 B |        0.93 |
| &#39;Get expired assessments&#39;                            | 776.0 ns | 312.2 ns | 17.12 ns |  0.81 |    0.02 |    1 | 0.0038 | 0.0029 |     400 B |        0.91 |
| &#39;Get all assessments&#39;                                | 754.7 ns | 609.6 ns | 33.41 ns |  0.79 |    0.03 |    1 | 0.0038 | 0.0029 |     400 B |        0.91 |
