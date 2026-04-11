```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 3.194 μs | 0.0438 μs | 0.0628 μs |  1.00 |    0.03 |    6 | 0.0725 | 0.0343 |    1312 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.133 μs | 0.0151 μs | 0.0217 μs |  0.35 |    0.01 |    1 | 0.0381 | 0.0191 |     688 B |        0.52 |
| &#39;Policy: get by ID&#39;                            | 2.414 μs | 0.0338 μs | 0.0496 μs |  0.76 |    0.02 |    4 | 0.0496 | 0.0229 |     928 B |        0.71 |
| &#39;Policy: deactivate&#39;                           | 2.081 μs | 0.0296 μs | 0.0425 μs |  0.65 |    0.02 |    2 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Record: track entity&#39;                         | 3.763 μs | 0.0229 μs | 0.0306 μs |  1.18 |    0.02 |    7 | 0.0648 | 0.0305 |    1152 B |        0.88 |
| &#39;Record: mark expired&#39;                         | 2.152 μs | 0.0131 μs | 0.0192 μs |  0.67 |    0.01 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.109 μs | 0.0074 μs | 0.0104 μs |  0.66 |    0.01 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.048 μs | 0.0099 μs | 0.0141 μs |  0.64 |    0.01 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.753 μs | 0.0217 μs | 0.0311 μs |  0.86 |    0.02 |    5 | 0.0572 | 0.0267 |    1024 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 2.291 μs | 0.0239 μs | 0.0343 μs |  0.72 |    0.02 |    3 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.145 μs | 0.0428 μs | 0.0614 μs |  0.36 |    0.02 |    1 | 0.0343 | 0.0172 |     608 B |        0.46 |
