```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.33GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 760.4 ns | 378.7 ns | 20.76 ns |  1.00 |    0.03 |    1 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 741.2 ns | 367.2 ns | 20.13 ns |  0.98 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 641.9 ns | 265.6 ns | 14.56 ns |  0.84 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            | 620.2 ns | 351.3 ns | 19.25 ns |  0.82 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 524.7 ns | 645.1 ns | 35.36 ns |  0.69 |    0.04 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
