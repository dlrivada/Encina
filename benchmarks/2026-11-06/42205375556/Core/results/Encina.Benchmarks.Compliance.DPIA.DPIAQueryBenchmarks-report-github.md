```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 707.0 ns | 404.21 ns | 22.16 ns |  1.00 |    0.04 |    1 | 0.0248 | 0.0238 |     440 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 600.1 ns |  96.73 ns |  5.30 ns |  0.85 |    0.02 |    1 | 0.0229 | 0.0219 |     408 B |        0.93 |
| &#39;Get expired assessments&#39;                            | 559.1 ns |  83.78 ns |  4.59 ns |  0.79 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.91 |
| &#39;Get all assessments&#39;                                | 535.7 ns | 264.69 ns | 14.51 ns |  0.76 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        0.91 |
