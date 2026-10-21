```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.82GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 3.447 μs | 1.5647 μs | 0.0858 μs |  1.00 |    0.03 |    2 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.210 μs | 0.5196 μs | 0.0285 μs |  0.35 |    0.01 |    1 | 0.0381 | 0.0191 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.607 μs | 0.4940 μs | 0.0271 μs |  0.76 |    0.02 |    2 | 0.0496 | 0.0229 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 2.240 μs | 0.8417 μs | 0.0461 μs |  0.65 |    0.02 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 4.025 μs | 1.1393 μs | 0.0624 μs |  1.17 |    0.03 |    2 | 0.0610 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 2.239 μs | 0.1087 μs | 0.0060 μs |  0.65 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.216 μs | 0.2610 μs | 0.0143 μs |  0.64 |    0.01 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.213 μs | 0.6293 μs | 0.0345 μs |  0.64 |    0.02 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.844 μs | 0.2624 μs | 0.0144 μs |  0.83 |    0.02 |    2 | 0.0572 | 0.0267 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 2.291 μs | 0.5515 μs | 0.0302 μs |  0.66 |    0.02 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.102 μs | 0.1611 μs | 0.0088 μs |  0.32 |    0.01 |    1 | 0.0343 | 0.0172 |     576 B |        0.46 |
