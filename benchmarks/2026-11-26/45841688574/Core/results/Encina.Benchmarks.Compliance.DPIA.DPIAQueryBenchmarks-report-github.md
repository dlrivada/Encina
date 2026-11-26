```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,037.0 ns | 1,166.8 ns | 63.95 ns |  1.00 |    0.07 |    2 | 0.0048 | 0.0038 |     440 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   843.2 ns |   486.2 ns | 26.65 ns |  0.82 |    0.05 |    1 | 0.0038 | 0.0029 |     408 B |        0.93 |
| &#39;Get expired assessments&#39;                            |   789.1 ns |   260.4 ns | 14.27 ns |  0.76 |    0.04 |    1 | 0.0038 | 0.0029 |     400 B |        0.91 |
| &#39;Get all assessments&#39;                                |   760.3 ns |   635.8 ns | 34.85 ns |  0.73 |    0.05 |    1 | 0.0038 | 0.0029 |     400 B |        0.91 |
