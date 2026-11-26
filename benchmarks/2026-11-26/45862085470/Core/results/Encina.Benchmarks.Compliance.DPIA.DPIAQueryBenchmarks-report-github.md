```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,202.4 ns | 2,627.2 ns | 144.01 ns |  1.01 |    0.14 |    1 | 0.0248 | 0.0229 |     424 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   972.9 ns |   351.2 ns |  19.25 ns |  0.82 |    0.08 |    1 | 0.0229 | 0.0210 |     392 B |        0.92 |
| &#39;Get expired assessments&#39;                            |   913.2 ns |   159.3 ns |   8.73 ns |  0.77 |    0.07 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
| &#39;Get all assessments&#39;                                |   881.8 ns |   185.6 ns |  10.17 ns |  0.74 |    0.07 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
