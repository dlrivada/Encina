```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.34GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         |   993.6 ns | 2,293.5 ns | 125.72 ns |  1.01 |    0.16 |    2 | 0.0038 | 0.0019 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,080.0 ns |   519.5 ns |  28.48 ns |  1.10 |    0.12 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetDPA by ID (cached read)&#39;              |   996.4 ns |   475.8 ns |  26.08 ns |  1.01 |    0.11 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;            |   997.7 ns |   897.0 ns |  49.17 ns |  1.01 |    0.12 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   784.4 ns |   106.8 ns |   5.85 ns |  0.80 |    0.09 |    1 | 0.0038 | 0.0029 |     400 B |        1.14 |
