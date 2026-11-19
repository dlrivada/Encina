```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 917.0 ns | 132.2 ns |  7.25 ns |  1.00 |    0.01 |    2 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 961.4 ns | 426.0 ns | 23.35 ns |  1.05 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.15 |
| &#39;GetDPA by ID (cached read)&#39;              | 851.8 ns | 178.5 ns |  9.78 ns |  0.93 |    0.01 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            | 815.7 ns | 124.6 ns |  6.83 ns |  0.89 |    0.01 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 658.7 ns | 106.1 ns |  5.81 ns |  0.72 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
