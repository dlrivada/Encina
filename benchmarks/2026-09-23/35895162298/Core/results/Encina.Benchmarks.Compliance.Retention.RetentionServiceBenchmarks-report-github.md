```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 3.107 μs | 3.4681 μs | 0.1901 μs |  1.00 |    0.07 |    2 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.118 μs | 0.7592 μs | 0.0416 μs |  0.36 |    0.02 |    1 | 0.0381 | 0.0191 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.425 μs | 0.3922 μs | 0.0215 μs |  0.78 |    0.04 |    2 | 0.0496 | 0.0229 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 2.119 μs | 1.3763 μs | 0.0754 μs |  0.68 |    0.04 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3.487 μs | 0.5765 μs | 0.0316 μs |  1.12 |    0.06 |    2 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 2.115 μs | 0.1401 μs | 0.0077 μs |  0.68 |    0.04 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.134 μs | 0.4764 μs | 0.0261 μs |  0.69 |    0.04 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.063 μs | 0.0500 μs | 0.0027 μs |  0.67 |    0.03 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.571 μs | 0.3117 μs | 0.0171 μs |  0.83 |    0.04 |    2 | 0.0572 | 0.0267 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 2.150 μs | 0.5188 μs | 0.0284 μs |  0.69 |    0.04 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.171 μs | 1.5284 μs | 0.0838 μs |  0.38 |    0.03 |    1 | 0.0343 | 0.0172 |     576 B |        0.46 |
