```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2,035.0 ns | 2,652.98 ns | 145.42 ns |  1.00 |    0.09 |    3 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   796.3 ns |   898.48 ns |  49.25 ns |  0.39 |    0.03 |    2 | 0.0391 | 0.0191 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,800.5 ns |    42.65 ns |   2.34 ns |  0.89 |    0.05 |    3 | 0.0515 | 0.0248 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,573.1 ns |   960.66 ns |  52.66 ns |  0.78 |    0.05 |    3 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,596.0 ns | 1,387.72 ns |  76.07 ns |  1.28 |    0.08 |    4 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,525.0 ns |   532.29 ns |  29.18 ns |  0.75 |    0.05 |    3 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,540.4 ns |   575.14 ns |  31.53 ns |  0.76 |    0.05 |    3 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,473.8 ns | 1,110.52 ns |  60.87 ns |  0.73 |    0.05 |    3 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,559.6 ns |    95.53 ns |   5.24 ns |  0.77 |    0.05 |    3 | 0.0572 | 0.0286 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,474.0 ns | 1,610.05 ns |  88.25 ns |  0.73 |    0.06 |    3 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   595.6 ns |    75.75 ns |   4.15 ns |  0.29 |    0.02 |    1 | 0.0343 | 0.0172 |     592 B |        0.47 |
