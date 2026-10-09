```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.772 μs | 0.3640 μs | 0.0200 μs |  1.00 |    3 | 0.0496 | 0.0229 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.336 μs | 0.0134 μs | 0.0007 μs |  0.48 |    1 | 0.0248 | 0.0114 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.232 μs | 0.2942 μs | 0.0161 μs |  0.81 |    2 | 0.0343 | 0.0305 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1.856 μs | 0.1549 μs | 0.0085 μs |  0.67 |    2 | 0.0267 | 0.0248 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3.002 μs | 0.3762 μs | 0.0206 μs |  1.08 |    3 | 0.0420 | 0.0191 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1.846 μs | 0.0933 μs | 0.0051 μs |  0.67 |    2 | 0.0229 | 0.0210 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1.825 μs | 0.2881 μs | 0.0158 μs |  0.66 |    2 | 0.0229 | 0.0210 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1.810 μs | 0.1861 μs | 0.0102 μs |  0.65 |    2 | 0.0229 | 0.0210 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.277 μs | 0.2355 μs | 0.0129 μs |  0.82 |    2 | 0.0381 | 0.0191 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1.901 μs | 0.3565 μs | 0.0195 μs |  0.69 |    2 | 0.0267 | 0.0229 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.250 μs | 0.2274 μs | 0.0125 μs |  0.45 |    1 | 0.0229 | 0.0210 |     576 B |        0.46 |
