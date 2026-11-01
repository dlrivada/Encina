```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1,282.7 ns | 23.37 ns | 33.51 ns |  1.00 |    0.04 |    3 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,333.0 ns | 19.44 ns | 29.10 ns |  1.04 |    0.03 |    3 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetDPA by ID (cached read)&#39;              | 1,161.8 ns | 24.31 ns | 36.38 ns |  0.91 |    0.04 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;            | 1,114.4 ns | 14.98 ns | 21.49 ns |  0.87 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   933.1 ns | 28.82 ns | 40.39 ns |  0.73 |    0.04 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
