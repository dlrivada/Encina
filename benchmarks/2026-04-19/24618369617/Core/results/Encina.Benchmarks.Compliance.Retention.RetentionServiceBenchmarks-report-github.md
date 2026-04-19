```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 3.164 μs | 0.0199 μs | 0.0291 μs |  1.00 |    0.01 |    6 | 0.0725 | 0.0343 |    1312 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.104 μs | 0.0078 μs | 0.0115 μs |  0.35 |    0.00 |    2 | 0.0381 | 0.0191 |     688 B |        0.52 |
| &#39;Policy: get by ID&#39;                            | 2.403 μs | 0.0152 μs | 0.0217 μs |  0.76 |    0.01 |    4 | 0.0496 | 0.0229 |     928 B |        0.71 |
| &#39;Policy: deactivate&#39;                           | 2.070 μs | 0.0133 μs | 0.0194 μs |  0.65 |    0.01 |    3 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Record: track entity&#39;                         | 3.679 μs | 0.0280 μs | 0.0401 μs |  1.16 |    0.02 |    7 | 0.0648 | 0.0305 |    1152 B |        0.88 |
| &#39;Record: mark expired&#39;                         | 2.130 μs | 0.0067 μs | 0.0093 μs |  0.67 |    0.01 |    3 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark deleted (terminal)&#39;              | 2.093 μs | 0.0106 μs | 0.0148 μs |  0.66 |    0.01 |    3 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Record: mark anonymized (terminal)&#39;           | 2.076 μs | 0.0096 μs | 0.0135 μs |  0.66 |    0.01 |    3 | 0.0343 | 0.0153 |     672 B |        0.51 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.707 μs | 0.0156 μs | 0.0233 μs |  0.86 |    0.01 |    5 | 0.0572 | 0.0267 |    1024 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 2.142 μs | 0.0150 μs | 0.0224 μs |  0.68 |    0.01 |    3 | 0.0381 | 0.0191 |     744 B |        0.57 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.062 μs | 0.0100 μs | 0.0146 μs |  0.34 |    0.01 |    1 | 0.0343 | 0.0172 |     608 B |        0.46 |
