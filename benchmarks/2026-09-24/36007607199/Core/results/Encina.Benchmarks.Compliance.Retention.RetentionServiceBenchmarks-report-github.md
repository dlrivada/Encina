```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.760 μs | 0.4923 μs | 0.0270 μs |  1.00 |    0.01 |    2 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.055 μs | 0.0265 μs | 0.0015 μs |  0.38 |    0.00 |    1 | 0.0381 | 0.0191 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.191 μs | 0.9322 μs | 0.0511 μs |  0.79 |    0.02 |    2 | 0.0496 | 0.0229 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1.995 μs | 0.6783 μs | 0.0372 μs |  0.72 |    0.01 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3.251 μs | 0.2827 μs | 0.0155 μs |  1.18 |    0.01 |    2 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1.985 μs | 0.4214 μs | 0.0231 μs |  0.72 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1.999 μs | 0.7932 μs | 0.0435 μs |  0.72 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1.917 μs | 0.1824 μs | 0.0100 μs |  0.69 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.332 μs | 0.2893 μs | 0.0159 μs |  0.84 |    0.01 |    2 | 0.0572 | 0.0267 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 2.012 μs | 0.0518 μs | 0.0028 μs |  0.73 |    0.01 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.030 μs | 0.1897 μs | 0.0104 μs |  0.37 |    0.00 |    1 | 0.0343 | 0.0172 |     576 B |        0.46 |
