```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean     | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 713.9 ns | 1,164.13 ns | 63.81 ns |  1.01 |    0.11 |    1 | 0.0248 | 0.0238 |     440 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 601.0 ns |    64.42 ns |  3.53 ns |  0.85 |    0.06 |    1 | 0.0229 | 0.0219 |     408 B |        0.93 |
| &#39;Get expired assessments&#39;                            | 564.7 ns |   306.08 ns | 16.78 ns |  0.80 |    0.06 |    1 | 0.0229 | 0.0219 |     400 B |        0.91 |
| &#39;Get all assessments&#39;                                | 528.3 ns |   280.75 ns | 15.39 ns |  0.74 |    0.06 |    1 | 0.0229 | 0.0219 |     400 B |        0.91 |
