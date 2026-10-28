```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1,017.3 ns | 328.3 ns | 17.99 ns |  1.00 |    0.02 |    2 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,085.8 ns | 330.1 ns | 18.10 ns |  1.07 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              |   944.1 ns | 486.2 ns | 26.65 ns |  0.93 |    0.03 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            |   934.6 ns | 178.7 ns |  9.79 ns |  0.92 |    0.02 |    2 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   770.7 ns | 274.5 ns | 15.05 ns |  0.76 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |
