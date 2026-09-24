```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.873 μs | 0.4414 μs | 0.0242 μs |  1.00 |    3 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.097 μs | 0.2748 μs | 0.0151 μs |  0.38 |    1 | 0.0381 | 0.0191 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.290 μs | 0.7298 μs | 0.0400 μs |  0.80 |    2 | 0.0496 | 0.0229 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1.977 μs | 0.5623 μs | 0.0308 μs |  0.69 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3.264 μs | 0.3525 μs | 0.0193 μs |  1.14 |    3 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1.994 μs | 0.0832 μs | 0.0046 μs |  0.69 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1.986 μs | 0.2012 μs | 0.0110 μs |  0.69 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1.951 μs | 0.3200 μs | 0.0175 μs |  0.68 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.370 μs | 0.0653 μs | 0.0036 μs |  0.83 |    2 | 0.0572 | 0.0267 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 2.040 μs | 0.2882 μs | 0.0158 μs |  0.71 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.014 μs | 0.1373 μs | 0.0075 μs |  0.35 |    1 | 0.0343 | 0.0172 |     576 B |        0.46 |
