```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         |   980.0 ns | 528.58 ns | 28.97 ns |  1.00 |    0.04 |    2 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,030.8 ns | 429.17 ns | 23.52 ns |  1.05 |    0.03 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              |   841.5 ns | 159.97 ns |  8.77 ns |  0.86 |    0.02 |    2 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;            |   832.5 ns |  51.25 ns |  2.81 ns |  0.85 |    0.02 |    2 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   685.7 ns | 308.70 ns | 16.92 ns |  0.70 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
