```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         |   984.6 ns | 2,261.78 ns | 123.98 ns |  1.01 |    0.16 |    1 | 0.0038 | 0.0029 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,087.7 ns |    28.15 ns |   1.54 ns |  1.12 |    0.13 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              |   979.9 ns |   183.08 ns |  10.04 ns |  1.01 |    0.12 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            |   906.2 ns |   739.06 ns |  40.51 ns |  0.93 |    0.11 |    1 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   779.6 ns |   756.79 ns |  41.48 ns |  0.80 |    0.10 |    1 | 0.0038 | 0.0029 |     400 B |        1.09 |
