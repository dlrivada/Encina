```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 3.403 μs | 0.0122 μs | 0.0007 μs |  1.00 |    3 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.172 μs | 0.3436 μs | 0.0188 μs |  0.34 |    1 | 0.0381 | 0.0191 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.414 μs | 0.3309 μs | 0.0181 μs |  0.71 |    2 | 0.0496 | 0.0229 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 2.156 μs | 0.3220 μs | 0.0177 μs |  0.63 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3.932 μs | 0.6124 μs | 0.0336 μs |  1.16 |    3 | 0.0610 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 2.211 μs | 0.3319 μs | 0.0182 μs |  0.65 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.103 μs | 0.1999 μs | 0.0110 μs |  0.62 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.084 μs | 0.3008 μs | 0.0165 μs |  0.61 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.767 μs | 0.3661 μs | 0.0201 μs |  0.81 |    2 | 0.0572 | 0.0267 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 2.198 μs | 0.3942 μs | 0.0216 μs |  0.65 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.085 μs | 0.3030 μs | 0.0166 μs |  0.32 |    1 | 0.0343 | 0.0172 |     576 B |        0.46 |
