```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 3.028 μs | 1.3577 μs | 0.0744 μs |  1.00 |    0.03 |    3 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.207 μs | 0.5929 μs | 0.0325 μs |  0.40 |    0.01 |    1 | 0.0381 | 0.0191 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.303 μs | 0.4096 μs | 0.0225 μs |  0.76 |    0.02 |    2 | 0.0496 | 0.0229 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 2.025 μs | 0.6304 μs | 0.0346 μs |  0.67 |    0.02 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3.490 μs | 0.5197 μs | 0.0285 μs |  1.15 |    0.03 |    3 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 2.032 μs | 0.7597 μs | 0.0416 μs |  0.67 |    0.02 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.024 μs | 0.3497 μs | 0.0192 μs |  0.67 |    0.02 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.037 μs | 1.1779 μs | 0.0646 μs |  0.67 |    0.02 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.478 μs | 0.2484 μs | 0.0136 μs |  0.82 |    0.02 |    2 | 0.0572 | 0.0267 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 2.052 μs | 0.6467 μs | 0.0354 μs |  0.68 |    0.02 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.106 μs | 0.2381 μs | 0.0131 μs |  0.37 |    0.01 |    1 | 0.0343 | 0.0172 |     576 B |        0.46 |
