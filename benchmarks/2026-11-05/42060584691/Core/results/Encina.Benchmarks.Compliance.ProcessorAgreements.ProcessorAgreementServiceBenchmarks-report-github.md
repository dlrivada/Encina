```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 755.2 ns | 234.49 ns | 12.85 ns |  1.00 |    0.02 |    1 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 760.0 ns |  29.72 ns |  1.63 ns |  1.01 |    0.01 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 667.0 ns | 697.58 ns | 38.24 ns |  0.88 |    0.05 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            | 633.6 ns | 364.38 ns | 19.97 ns |  0.84 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 551.1 ns | 687.59 ns | 37.69 ns |  0.73 |    0.04 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |
