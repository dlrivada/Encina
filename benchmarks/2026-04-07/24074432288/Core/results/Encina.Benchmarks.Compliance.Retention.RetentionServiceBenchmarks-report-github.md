```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.698 μs | 0.0080 μs | 0.0120 μs |  1.00 |    4 | 0.0725 | 0.0343 |    1312 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.042 μs | 0.0087 μs | 0.0125 μs |  0.39 |    1 | 0.0381 | 0.0191 |     688 B |        0.52 |
| &#39;Policy: get by ID&#39;                            | 2.214 μs | 0.0154 μs | 0.0216 μs |  0.82 |    3 | 0.0496 | 0.0229 |     928 B |        0.71 |
| &#39;Policy: deactivate&#39;                           | 1.980 μs | 0.0199 μs | 0.0286 μs |  0.73 |    2 | 0.0401 | 0.0191 |     712 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3.129 μs | 0.0073 μs | 0.0102 μs |  1.16 |    5 | 0.0648 | 0.0305 |    1152 B |        0.88 |
| &#39;Record: mark expired&#39;                         | 1.927 μs | 0.0068 μs | 0.0102 μs |  0.71 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark deleted (terminal)&#39;              | 1.934 μs | 0.0074 μs | 0.0111 μs |  0.72 |    2 | 0.0362 | 0.0172 |     640 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1.931 μs | 0.0141 μs | 0.0207 μs |  0.72 |    2 | 0.0362 | 0.0172 |     640 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.280 μs | 0.0093 μs | 0.0127 μs |  0.85 |    3 | 0.0572 | 0.0267 |    1024 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 1.944 μs | 0.0108 μs | 0.0158 μs |  0.72 |    2 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.021 μs | 0.0073 μs | 0.0102 μs |  0.38 |    1 | 0.0343 | 0.0172 |     608 B |        0.46 |
