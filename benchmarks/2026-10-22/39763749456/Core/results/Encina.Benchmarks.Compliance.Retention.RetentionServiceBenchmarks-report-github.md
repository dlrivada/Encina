```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 3.513 μs | 0.1381 μs | 0.0076 μs |  1.00 |    0.00 |    3 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.207 μs | 0.2026 μs | 0.0111 μs |  0.34 |    0.00 |    1 | 0.0381 | 0.0191 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 2.474 μs | 0.3941 μs | 0.0216 μs |  0.70 |    0.01 |    2 | 0.0496 | 0.0229 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 2.205 μs | 0.3689 μs | 0.0202 μs |  0.63 |    0.01 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3.884 μs | 1.0374 μs | 0.0569 μs |  1.11 |    0.01 |    3 | 0.0610 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 2.234 μs | 0.1424 μs | 0.0078 μs |  0.64 |    0.00 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.209 μs | 0.2173 μs | 0.0119 μs |  0.63 |    0.00 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.163 μs | 0.3116 μs | 0.0171 μs |  0.62 |    0.00 |    2 | 0.0343 | 0.0153 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.896 μs | 1.7413 μs | 0.0954 μs |  0.82 |    0.02 |    2 | 0.0572 | 0.0267 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 2.236 μs | 1.6944 μs | 0.0929 μs |  0.64 |    0.02 |    2 | 0.0381 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.139 μs | 0.8097 μs | 0.0444 μs |  0.32 |    0.01 |    1 | 0.0343 | 0.0172 |     576 B |        0.46 |
