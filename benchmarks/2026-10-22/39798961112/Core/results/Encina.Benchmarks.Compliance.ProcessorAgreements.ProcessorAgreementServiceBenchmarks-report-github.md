```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 771.3 ns | 32.33 ns | 48.39 ns |  1.00 |    0.09 |    2 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 798.7 ns | 22.02 ns | 32.27 ns |  1.04 |    0.08 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 741.7 ns | 23.61 ns | 35.33 ns |  0.97 |    0.07 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            | 735.3 ns | 20.58 ns | 30.80 ns |  0.96 |    0.07 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 596.6 ns | 23.45 ns | 34.38 ns |  0.78 |    0.06 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
