```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 3.648 μs | 0.0098 μs | 0.0144 μs |  1.00 |    7 | 0.0725 | 0.0343 |    1312 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.267 μs | 0.0048 μs | 0.0071 μs |  0.35 |    2 | 0.0381 | 0.0191 |     688 B |        0.52 |
| &#39;Policy: get by ID&#39;                            | 2.745 μs | 0.0205 μs | 0.0301 μs |  0.75 |    5 | 0.0496 | 0.0229 |     928 B |        0.71 |
| &#39;Policy: deactivate&#39;                           | 2.381 μs | 0.0143 μs | 0.0214 μs |  0.65 |    3 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Record: track entity&#39;                         | 4.290 μs | 0.0153 μs | 0.0230 μs |  1.18 |    8 | 0.0610 | 0.0305 |    1088 B |        0.83 |
| &#39;Record: mark expired&#39;                         | 2.404 μs | 0.0035 μs | 0.0051 μs |  0.66 |    3 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.383 μs | 0.0051 μs | 0.0073 μs |  0.65 |    3 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.378 μs | 0.0116 μs | 0.0166 μs |  0.65 |    3 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 3.127 μs | 0.0159 μs | 0.0233 μs |  0.86 |    6 | 0.0572 | 0.0267 |    1024 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 2.525 μs | 0.0316 μs | 0.0472 μs |  0.69 |    4 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.196 μs | 0.0085 μs | 0.0127 μs |  0.33 |    1 | 0.0343 | 0.0172 |     608 B |        0.46 |
