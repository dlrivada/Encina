```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1,215.7 ns | 17.60 ns | 26.35 ns |  1.00 |    0.03 |    3 | 0.0210 | 0.0191 |     384 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,234.0 ns | 19.12 ns | 28.03 ns |  1.02 |    0.03 |    3 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetDPA by ID (cached read)&#39;              | 1,065.4 ns | 20.52 ns | 30.71 ns |  0.88 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetActiveDPA by processor ID&#39;            | 1,053.0 ns | 15.31 ns | 21.95 ns |  0.87 |    0.03 |    2 | 0.0248 | 0.0229 |     456 B |        1.19 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   880.7 ns | 15.67 ns | 23.46 ns |  0.72 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.04 |
