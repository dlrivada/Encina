```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.953 μs | 0.8742 μs | 0.0479 μs |  1.00 |    0.02 |    2 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.164 μs | 0.3652 μs | 0.0200 μs |  0.39 |    0.01 |    1 | 0.0381 | 0.0191 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.443 μs | 0.2481 μs | 0.0136 μs |  0.83 |    0.01 |    2 | 0.0496 | 0.0229 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 2.133 μs | 0.2106 μs | 0.0115 μs |  0.72 |    0.01 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3.444 μs | 0.5583 μs | 0.0306 μs |  1.17 |    0.02 |    2 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 2.142 μs | 0.3739 μs | 0.0205 μs |  0.73 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.104 μs | 0.2387 μs | 0.0131 μs |  0.71 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.086 μs | 0.1138 μs | 0.0062 μs |  0.71 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.526 μs | 0.5654 μs | 0.0310 μs |  0.86 |    0.02 |    2 | 0.0572 | 0.0267 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 2.186 μs | 0.2553 μs | 0.0140 μs |  0.74 |    0.01 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.141 μs | 0.7098 μs | 0.0389 μs |  0.39 |    0.01 |    1 | 0.0343 | 0.0172 |     576 B |        0.46 |
