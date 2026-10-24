```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 761.6 ns |   102.49 ns |  5.62 ns |  1.00 |    0.01 |    2 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 816.1 ns |    38.84 ns |  2.13 ns |  1.07 |    0.01 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 735.8 ns |   568.22 ns | 31.15 ns |  0.97 |    0.04 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            | 725.4 ns |   491.68 ns | 26.95 ns |  0.95 |    0.03 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 605.5 ns | 1,031.02 ns | 56.51 ns |  0.80 |    0.06 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
