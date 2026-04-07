```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.759 μs | 0.0293 μs | 0.0439 μs |  1.00 |    0.02 |    6 | 0.0725 | 0.0343 |    1312 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.117 μs | 0.0100 μs | 0.0143 μs |  0.40 |    0.01 |    2 | 0.0381 | 0.0191 |     688 B |        0.52 |
| &#39;Policy: get by ID&#39;                            | 2.224 μs | 0.0189 μs | 0.0283 μs |  0.81 |    0.02 |    4 | 0.0496 | 0.0229 |     928 B |        0.71 |
| &#39;Policy: deactivate&#39;                           | 1.925 μs | 0.0136 μs | 0.0203 μs |  0.70 |    0.01 |    3 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Record: track entity&#39;                         | 3.214 μs | 0.0126 μs | 0.0173 μs |  1.17 |    0.02 |    7 | 0.0648 | 0.0305 |    1152 B |        0.88 |
| &#39;Record: mark expired&#39;                         | 2.025 μs | 0.0220 μs | 0.0329 μs |  0.73 |    0.02 |    3 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark deleted (terminal)&#39;              | 1.961 μs | 0.0148 μs | 0.0208 μs |  0.71 |    0.01 |    3 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1.965 μs | 0.0307 μs | 0.0460 μs |  0.71 |    0.02 |    3 | 0.0362 | 0.0172 |     640 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.333 μs | 0.0162 μs | 0.0232 μs |  0.85 |    0.02 |    5 | 0.0572 | 0.0267 |    1024 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 1.985 μs | 0.0186 μs | 0.0273 μs |  0.72 |    0.01 |    3 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.034 μs | 0.0127 μs | 0.0179 μs |  0.37 |    0.01 |    1 | 0.0343 | 0.0172 |     608 B |        0.46 |
