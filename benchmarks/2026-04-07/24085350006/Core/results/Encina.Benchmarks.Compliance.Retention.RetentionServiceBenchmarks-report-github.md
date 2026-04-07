```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.858 μs | 0.4223 μs | 0.0231 μs |  1.00 |    2 | 0.0496 | 0.0229 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.353 μs | 0.1492 μs | 0.0082 μs |  0.47 |    1 | 0.0248 | 0.0114 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.276 μs | 0.2720 μs | 0.0149 μs |  0.80 |    2 | 0.0343 | 0.0305 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1.913 μs | 0.1286 μs | 0.0070 μs |  0.67 |    2 | 0.0267 | 0.0229 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3.046 μs | 0.1883 μs | 0.0103 μs |  1.07 |    2 | 0.0420 | 0.0191 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1.882 μs | 0.1549 μs | 0.0085 μs |  0.66 |    2 | 0.0229 | 0.0210 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1.873 μs | 0.1073 μs | 0.0059 μs |  0.66 |    2 | 0.0229 | 0.0210 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1.841 μs | 0.1602 μs | 0.0088 μs |  0.64 |    2 | 0.0229 | 0.0210 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.385 μs | 0.0915 μs | 0.0050 μs |  0.83 |    2 | 0.0381 | 0.0191 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1.953 μs | 0.2430 μs | 0.0133 μs |  0.68 |    2 | 0.0267 | 0.0229 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.310 μs | 0.0620 μs | 0.0034 μs |  0.46 |    1 | 0.0229 | 0.0210 |     576 B |        0.46 |
