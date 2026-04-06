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
| &#39;Policy: create (fast path)&#39;                   | 3.015 μs | 0.0541 μs | 0.0810 μs |  1.00 |    0.04 |    5 | 0.0725 | 0.0343 |    1312 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.127 μs | 0.0214 μs | 0.0314 μs |  0.37 |    0.01 |    1 | 0.0381 | 0.0191 |     688 B |        0.52 |
| &#39;Policy: get by ID&#39;                            | 2.292 μs | 0.0359 μs | 0.0515 μs |  0.76 |    0.03 |    3 | 0.0496 | 0.0229 |     928 B |        0.71 |
| &#39;Policy: deactivate&#39;                           | 2.010 μs | 0.0227 μs | 0.0326 μs |  0.67 |    0.02 |    2 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Record: track entity&#39;                         | 3.428 μs | 0.0433 μs | 0.0648 μs |  1.14 |    0.04 |    6 | 0.0648 | 0.0305 |    1152 B |        0.88 |
| &#39;Record: mark expired&#39;                         | 2.055 μs | 0.0284 μs | 0.0425 μs |  0.68 |    0.02 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark deleted (terminal)&#39;              | 1.954 μs | 0.0239 μs | 0.0343 μs |  0.65 |    0.02 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1.982 μs | 0.0294 μs | 0.0440 μs |  0.66 |    0.02 |    2 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.398 μs | 0.0133 μs | 0.0199 μs |  0.80 |    0.02 |    4 | 0.0572 | 0.0267 |    1024 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 2.069 μs | 0.0197 μs | 0.0295 μs |  0.69 |    0.02 |    2 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.137 μs | 0.0247 μs | 0.0362 μs |  0.38 |    0.02 |    1 | 0.0343 | 0.0172 |     608 B |        0.46 |
