```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|---------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1,402.7 ns | 96.23 ns | 144.03 ns |  1.01 |    0.15 |    3 | 0.0038 | 0.0019 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,135.4 ns | 40.77 ns |  61.02 ns |  0.82 |    0.10 |    2 | 0.0038 | 0.0019 |     456 B |        1.19 |
| &#39;GetDPA by ID (cached read)&#39;              |   988.5 ns | 35.14 ns |  52.59 ns |  0.71 |    0.08 |    1 | 0.0038 | 0.0019 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;            |   969.7 ns | 35.76 ns |  53.53 ns |  0.70 |    0.08 |    1 | 0.0038 | 0.0019 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 1,164.0 ns | 48.42 ns |  72.48 ns |  0.84 |    0.10 |    2 | 0.0038 | 0.0029 |     400 B |        1.04 |
