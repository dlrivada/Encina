```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 3.420 μs | 0.9540 μs | 0.0523 μs |  1.00 |    0.02 |    3 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.151 μs | 0.2571 μs | 0.0141 μs |  0.34 |    0.01 |    1 | 0.0381 | 0.0191 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.397 μs | 0.7290 μs | 0.0400 μs |  0.70 |    0.01 |    2 | 0.0496 | 0.0229 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 2.143 μs | 0.2576 μs | 0.0141 μs |  0.63 |    0.01 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3.827 μs | 0.1397 μs | 0.0077 μs |  1.12 |    0.01 |    3 | 0.0610 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 2.154 μs | 0.2646 μs | 0.0145 μs |  0.63 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.104 μs | 0.0833 μs | 0.0046 μs |  0.62 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.088 μs | 0.1523 μs | 0.0083 μs |  0.61 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.804 μs | 1.2298 μs | 0.0674 μs |  0.82 |    0.02 |    2 | 0.0572 | 0.0267 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 2.212 μs | 0.5853 μs | 0.0321 μs |  0.65 |    0.01 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.094 μs | 0.1867 μs | 0.0102 μs |  0.32 |    0.00 |    1 | 0.0343 | 0.0172 |     576 B |        0.46 |
