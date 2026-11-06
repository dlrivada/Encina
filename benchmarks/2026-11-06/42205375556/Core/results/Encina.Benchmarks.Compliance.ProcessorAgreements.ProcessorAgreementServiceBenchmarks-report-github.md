```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 958.8 ns | 568.5 ns | 31.16 ns |  1.00 |    0.04 |    2 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 987.2 ns | 135.9 ns |  7.45 ns |  1.03 |    0.03 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 891.1 ns | 328.3 ns | 17.99 ns |  0.93 |    0.03 |    2 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;            | 816.0 ns | 120.5 ns |  6.61 ns |  0.85 |    0.02 |    2 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 673.8 ns | 204.1 ns | 11.19 ns |  0.70 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
