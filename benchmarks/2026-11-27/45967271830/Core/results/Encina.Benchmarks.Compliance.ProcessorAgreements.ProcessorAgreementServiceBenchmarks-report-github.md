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
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1,068.5 ns | 426.5 ns | 23.38 ns |  1.00 |    0.03 |    2 | 0.0038 | 0.0029 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,071.0 ns | 556.9 ns | 30.52 ns |  1.00 |    0.03 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              |   955.5 ns | 152.2 ns |  8.34 ns |  0.89 |    0.02 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            |   953.3 ns | 329.8 ns | 18.08 ns |  0.89 |    0.02 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   776.7 ns | 350.0 ns | 19.18 ns |  0.73 |    0.02 |    1 | 0.0038 | 0.0029 |     400 B |        1.09 |
