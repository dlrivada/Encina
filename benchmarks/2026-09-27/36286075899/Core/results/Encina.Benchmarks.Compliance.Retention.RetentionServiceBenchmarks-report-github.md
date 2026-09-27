```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.851 μs | 0.0618 μs | 0.0925 μs |  1.00 |    0.04 |    4 | 0.0725 | 0.0343 |    1312 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.076 μs | 0.0227 μs | 0.0333 μs |  0.38 |    0.02 |    1 | 0.0381 | 0.0191 |     688 B |        0.52 |
| &#39;Policy: get by ID&#39;                            | 2.276 μs | 0.0272 μs | 0.0399 μs |  0.80 |    0.03 |    3 | 0.0496 | 0.0229 |     928 B |        0.71 |
| &#39;Policy: deactivate&#39;                           | 1.968 μs | 0.0127 μs | 0.0191 μs |  0.69 |    0.02 |    2 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Record: track entity&#39;                         | 3.243 μs | 0.0120 μs | 0.0168 μs |  1.14 |    0.04 |    5 | 0.0648 | 0.0305 |    1152 B |        0.88 |
| &#39;Record: mark expired&#39;                         | 2.045 μs | 0.0213 μs | 0.0312 μs |  0.72 |    0.03 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.019 μs | 0.0198 μs | 0.0278 μs |  0.71 |    0.02 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1.982 μs | 0.0297 μs | 0.0426 μs |  0.70 |    0.03 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.347 μs | 0.0193 μs | 0.0277 μs |  0.82 |    0.03 |    3 | 0.0572 | 0.0267 |    1024 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 2.068 μs | 0.0252 μs | 0.0369 μs |  0.73 |    0.03 |    2 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.055 μs | 0.0131 μs | 0.0183 μs |  0.37 |    0.01 |    1 | 0.0343 | 0.0172 |     608 B |        0.46 |
