```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1,183.4 ns | 2,222.2 ns | 121.81 ns |  1.01 |    0.13 |    1 | 0.0038 | 0.0019 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,140.0 ns |   767.0 ns |  42.04 ns |  0.97 |    0.10 |    1 | 0.0038 | 0.0019 |     424 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              |   985.3 ns |   705.6 ns |  38.68 ns |  0.84 |    0.08 |    1 | 0.0038 | 0.0019 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            |   907.9 ns |   390.6 ns |  21.41 ns |  0.77 |    0.07 |    1 | 0.0038 | 0.0019 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   939.8 ns |   550.9 ns |  30.20 ns |  0.80 |    0.08 |    1 | 0.0038 | 0.0029 |     400 B |        1.14 |
