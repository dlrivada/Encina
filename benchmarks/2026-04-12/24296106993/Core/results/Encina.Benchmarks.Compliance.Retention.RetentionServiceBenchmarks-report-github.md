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
| &#39;Policy: create (fast path)&#39;                   | 3.218 μs | 0.0203 μs | 0.0292 μs |  1.00 |    0.01 |    5 | 0.0725 | 0.0343 |    1312 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.145 μs | 0.0161 μs | 0.0236 μs |  0.36 |    0.01 |    1 | 0.0381 | 0.0191 |     688 B |        0.52 |
| &#39;Policy: get by ID&#39;                            | 2.462 μs | 0.0342 μs | 0.0502 μs |  0.76 |    0.02 |    3 | 0.0496 | 0.0229 |     928 B |        0.71 |
| &#39;Policy: deactivate&#39;                           | 2.196 μs | 0.0273 μs | 0.0409 μs |  0.68 |    0.01 |    2 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Record: track entity&#39;                         | 3.975 μs | 0.0755 μs | 0.1106 μs |  1.24 |    0.04 |    6 | 0.0610 | 0.0305 |    1088 B |        0.83 |
| &#39;Record: mark expired&#39;                         | 2.180 μs | 0.0139 μs | 0.0208 μs |  0.68 |    0.01 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.118 μs | 0.0195 μs | 0.0280 μs |  0.66 |    0.01 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.114 μs | 0.0118 μs | 0.0177 μs |  0.66 |    0.01 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.837 μs | 0.0291 μs | 0.0435 μs |  0.88 |    0.02 |    4 | 0.0572 | 0.0267 |    1024 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 2.249 μs | 0.0217 μs | 0.0318 μs |  0.70 |    0.01 |    2 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.097 μs | 0.0162 μs | 0.0227 μs |  0.34 |    0.01 |    1 | 0.0343 | 0.0172 |     608 B |        0.46 |
