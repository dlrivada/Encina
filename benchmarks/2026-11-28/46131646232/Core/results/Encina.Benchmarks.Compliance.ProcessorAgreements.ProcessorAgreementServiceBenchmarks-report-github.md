```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 943.2 ns | 729.11 ns | 39.96 ns |  1.00 |    0.05 |    1 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 956.6 ns | 364.50 ns | 19.98 ns |  1.02 |    0.04 |    1 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 846.6 ns | 153.83 ns |  8.43 ns |  0.90 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;            | 817.2 ns |  25.46 ns |  1.40 ns |  0.87 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 693.2 ns | 327.71 ns | 17.96 ns |  0.74 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
