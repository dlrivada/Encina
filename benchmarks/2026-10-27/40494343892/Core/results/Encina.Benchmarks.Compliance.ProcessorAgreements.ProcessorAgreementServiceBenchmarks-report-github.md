```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         |   973.4 ns | 208.9 ns | 11.45 ns |  1.00 |    0.01 |    2 | 0.0038 | 0.0019 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,151.9 ns | 781.8 ns | 42.85 ns |  1.18 |    0.04 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetDPA by ID (cached read)&#39;              | 1,059.9 ns | 528.2 ns | 28.95 ns |  1.09 |    0.03 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;            | 1,014.6 ns | 130.3 ns |  7.14 ns |  1.04 |    0.01 |    2 | 0.0048 | 0.0038 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   775.3 ns | 459.2 ns | 25.17 ns |  0.80 |    0.02 |    1 | 0.0038 | 0.0029 |     400 B |        1.14 |
