```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1,051.8 ns | 934.0 ns | 51.19 ns |  1.00 |    0.06 |    2 | 0.0038 | 0.0029 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,081.0 ns | 125.0 ns |  6.85 ns |  1.03 |    0.04 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              |   933.8 ns | 390.3 ns | 21.39 ns |  0.89 |    0.04 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            |   957.9 ns | 156.7 ns |  8.59 ns |  0.91 |    0.04 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   727.5 ns | 434.2 ns | 23.80 ns |  0.69 |    0.04 |    1 | 0.0038 | 0.0029 |     400 B |        1.09 |
