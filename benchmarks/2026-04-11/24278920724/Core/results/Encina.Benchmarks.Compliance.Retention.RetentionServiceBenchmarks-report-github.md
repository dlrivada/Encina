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
| &#39;Policy: create (fast path)&#39;                   | 2.717 μs | 0.0127 μs | 0.0182 μs |  1.00 |    5 | 0.0725 | 0.0343 |    1312 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.046 μs | 0.0044 μs | 0.0063 μs |  0.39 |    1 | 0.0381 | 0.0191 |     688 B |        0.52 |
| &#39;Policy: get by ID&#39;                            | 2.230 μs | 0.0132 μs | 0.0186 μs |  0.82 |    3 | 0.0496 | 0.0229 |     928 B |        0.71 |
| &#39;Policy: deactivate&#39;                           | 1.921 μs | 0.0122 μs | 0.0179 μs |  0.71 |    2 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Record: track entity&#39;                         | 3.203 μs | 0.0134 μs | 0.0192 μs |  1.18 |    6 | 0.0648 | 0.0305 |    1152 B |        0.88 |
| &#39;Record: mark expired&#39;                         | 1.958 μs | 0.0078 μs | 0.0107 μs |  0.72 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark deleted (terminal)&#39;              | 1.936 μs | 0.0078 μs | 0.0115 μs |  0.71 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1.937 μs | 0.0215 μs | 0.0323 μs |  0.71 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.332 μs | 0.0226 μs | 0.0324 μs |  0.86 |    4 | 0.0572 | 0.0267 |    1024 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 1.981 μs | 0.0158 μs | 0.0237 μs |  0.73 |    2 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.025 μs | 0.0093 μs | 0.0139 μs |  0.38 |    1 | 0.0343 | 0.0172 |     608 B |        0.46 |
