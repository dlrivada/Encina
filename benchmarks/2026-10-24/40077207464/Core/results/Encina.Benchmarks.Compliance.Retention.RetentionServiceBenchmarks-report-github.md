```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.949 μs | 0.8145 μs | 0.0446 μs |  1.00 |    0.02 |    3 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.155 μs | 0.1682 μs | 0.0092 μs |  0.39 |    0.01 |    1 | 0.0381 | 0.0191 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.346 μs | 1.2430 μs | 0.0681 μs |  0.80 |    0.02 |    2 | 0.0496 | 0.0229 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1.965 μs | 0.1535 μs | 0.0084 μs |  0.67 |    0.01 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
