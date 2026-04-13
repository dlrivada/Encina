```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                         | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 3.369 μs | 0.0747 μs | 0.1118 μs | 3.368 μs |  1.00 |    0.05 |    7 | 0.0725 | 0.0343 |    1312 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.174 μs | 0.0303 μs | 0.0453 μs | 1.178 μs |  0.35 |    0.02 |    2 | 0.0381 | 0.0191 |     688 B |        0.52 |
| &#39;Policy: get by ID&#39;                            | 2.411 μs | 0.0264 μs | 0.0387 μs | 2.401 μs |  0.72 |    0.03 |    5 | 0.0496 | 0.0229 |     928 B |        0.71 |
| &#39;Policy: deactivate&#39;                           | 2.143 μs | 0.0359 μs | 0.0537 μs | 2.149 μs |  0.64 |    0.03 |    4 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Record: track entity&#39;                         | 3.831 μs | 0.0459 μs | 0.0687 μs | 3.815 μs |  1.14 |    0.04 |    8 | 0.0610 | 0.0305 |    1088 B |        0.83 |
| &#39;Record: mark expired&#39;                         | 2.127 μs | 0.0171 μs | 0.0256 μs | 2.114 μs |  0.63 |    0.02 |    4 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.084 μs | 0.0055 μs | 0.0082 μs | 2.083 μs |  0.62 |    0.02 |    4 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.030 μs | 0.0049 μs | 0.0072 μs | 2.030 μs |  0.60 |    0.02 |    3 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.727 μs | 0.0069 μs | 0.0101 μs | 2.727 μs |  0.81 |    0.03 |    6 | 0.0572 | 0.0267 |    1024 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 2.142 μs | 0.0160 μs | 0.0240 μs | 2.143 μs |  0.64 |    0.02 |    4 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.064 μs | 0.0128 μs | 0.0184 μs | 1.063 μs |  0.32 |    0.01 |    1 | 0.0343 | 0.0172 |     608 B |        0.46 |
