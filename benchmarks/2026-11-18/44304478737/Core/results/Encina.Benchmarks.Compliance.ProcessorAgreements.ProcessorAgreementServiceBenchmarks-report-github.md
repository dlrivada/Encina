```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         |   958.6 ns | 1,240.7 ns | 68.01 ns |  1.00 |    0.09 |    2 | 0.0038 | 0.0029 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,123.0 ns |   102.6 ns |  5.62 ns |  1.18 |    0.07 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 1,034.3 ns |   774.9 ns | 42.47 ns |  1.08 |    0.08 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            |   991.4 ns |   132.4 ns |  7.26 ns |  1.04 |    0.07 |    2 | 0.0048 | 0.0038 |     440 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   782.4 ns |   674.7 ns | 36.98 ns |  0.82 |    0.06 |    1 | 0.0038 | 0.0029 |     400 B |        1.09 |
