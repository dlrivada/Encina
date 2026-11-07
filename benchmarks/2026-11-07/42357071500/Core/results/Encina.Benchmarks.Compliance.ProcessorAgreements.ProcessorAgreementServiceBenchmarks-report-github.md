```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 686.2 ns | 422.4 ns | 23.16 ns |  1.00 |    0.04 |    2 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 704.0 ns | 258.7 ns | 14.18 ns |  1.03 |    0.03 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 626.9 ns | 500.0 ns | 27.41 ns |  0.91 |    0.04 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            | 618.7 ns | 171.0 ns |  9.37 ns |  0.90 |    0.03 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 503.6 ns | 185.6 ns | 10.17 ns |  0.73 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
