```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.731 μs | 0.0088 μs | 0.0126 μs |  1.00 |    0.01 |    5 | 0.0725 | 0.0343 |    1312 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.063 μs | 0.0109 μs | 0.0156 μs |  0.39 |    0.01 |    1 | 0.0381 | 0.0191 |     688 B |        0.52 |
| &#39;Policy: get by ID&#39;                            | 2.236 μs | 0.0139 μs | 0.0200 μs |  0.82 |    0.01 |    3 | 0.0496 | 0.0229 |     928 B |        0.71 |
| &#39;Policy: deactivate&#39;                           | 1.930 μs | 0.0113 μs | 0.0169 μs |  0.71 |    0.01 |    2 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Record: track entity&#39;                         | 3.226 μs | 0.0125 μs | 0.0187 μs |  1.18 |    0.01 |    6 | 0.0648 | 0.0305 |    1152 B |        0.88 |
| &#39;Record: mark expired&#39;                         | 1.954 μs | 0.0072 μs | 0.0103 μs |  0.72 |    0.00 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark deleted (terminal)&#39;              | 1.940 μs | 0.0123 μs | 0.0180 μs |  0.71 |    0.01 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1.954 μs | 0.0297 μs | 0.0444 μs |  0.72 |    0.02 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.364 μs | 0.0237 μs | 0.0355 μs |  0.87 |    0.01 |    4 | 0.0572 | 0.0267 |    1024 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 1.994 μs | 0.0162 μs | 0.0242 μs |  0.73 |    0.01 |    2 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.074 μs | 0.0276 μs | 0.0413 μs |  0.39 |    0.01 |    1 | 0.0343 | 0.0172 |     608 B |        0.46 |
