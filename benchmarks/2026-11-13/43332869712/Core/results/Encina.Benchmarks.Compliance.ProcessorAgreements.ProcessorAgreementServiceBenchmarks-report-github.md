```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1,178.3 ns | 200.2 ns | 10.97 ns |  1.00 |    0.01 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,233.4 ns | 275.5 ns | 15.10 ns |  1.05 |    0.01 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 1,043.7 ns | 188.7 ns | 10.34 ns |  0.89 |    0.01 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            |   992.5 ns | 377.8 ns | 20.71 ns |  0.84 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   867.6 ns | 285.1 ns | 15.62 ns |  0.74 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
