```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         |   896.0 ns | 2,659.4 ns | 145.77 ns |  1.02 |    0.20 |    1 | 0.0038 | 0.0019 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,085.4 ns |   783.4 ns |  42.94 ns |  1.23 |    0.17 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetDPA by ID (cached read)&#39;              | 1,028.2 ns | 1,196.3 ns |  65.57 ns |  1.17 |    0.17 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;            |   996.5 ns |   287.0 ns |  15.73 ns |  1.13 |    0.15 |    1 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   765.9 ns |   119.8 ns |   6.57 ns |  0.87 |    0.12 |    1 | 0.0038 | 0.0029 |     400 B |        1.14 |
