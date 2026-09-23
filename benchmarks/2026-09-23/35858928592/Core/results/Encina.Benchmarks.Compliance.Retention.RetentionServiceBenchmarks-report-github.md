```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 3.771 μs | 0.9723 μs | 0.0533 μs |  1.00 |    0.02 |    3 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.404 μs | 1.3920 μs | 0.0763 μs |  0.37 |    0.02 |    1 | 0.0381 | 0.0191 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.625 μs | 1.2404 μs | 0.0680 μs |  0.70 |    0.02 |    2 | 0.0496 | 0.0229 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 2.260 μs | 0.8770 μs | 0.0481 μs |  0.60 |    0.01 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3.952 μs | 0.9030 μs | 0.0495 μs |  1.05 |    0.02 |    3 | 0.0610 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 2.202 μs | 0.2139 μs | 0.0117 μs |  0.58 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.210 μs | 0.3646 μs | 0.0200 μs |  0.59 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.163 μs | 0.2208 μs | 0.0121 μs |  0.57 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.958 μs | 1.5243 μs | 0.0836 μs |  0.78 |    0.02 |    2 | 0.0572 | 0.0267 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 2.344 μs | 0.3658 μs | 0.0200 μs |  0.62 |    0.01 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.266 μs | 0.1848 μs | 0.0101 μs |  0.34 |    0.00 |    1 | 0.0343 | 0.0172 |     576 B |        0.46 |
