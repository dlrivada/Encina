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
| &#39;Policy: create (fast path)&#39;                   | 2.690 μs | 0.3458 μs | 0.0190 μs |  1.00 |    2 | 0.0496 | 0.0229 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.284 μs | 0.3944 μs | 0.0216 μs |  0.48 |    1 | 0.0248 | 0.0114 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.171 μs | 0.7148 μs | 0.0392 μs |  0.81 |    2 | 0.0343 | 0.0305 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1.802 μs | 0.4251 μs | 0.0233 μs |  0.67 |    2 | 0.0267 | 0.0248 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2.921 μs | 0.7572 μs | 0.0415 μs |  1.09 |    2 | 0.0420 | 0.0191 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1.789 μs | 0.4655 μs | 0.0255 μs |  0.67 |    2 | 0.0229 | 0.0210 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1.755 μs | 0.0928 μs | 0.0051 μs |  0.65 |    2 | 0.0229 | 0.0210 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1.780 μs | 0.2066 μs | 0.0113 μs |  0.66 |    2 | 0.0229 | 0.0210 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.256 μs | 0.1924 μs | 0.0105 μs |  0.84 |    2 | 0.0381 | 0.0191 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1.855 μs | 0.2547 μs | 0.0140 μs |  0.69 |    2 | 0.0267 | 0.0248 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.213 μs | 0.5887 μs | 0.0323 μs |  0.45 |    1 | 0.0229 | 0.0210 |     576 B |        0.46 |
