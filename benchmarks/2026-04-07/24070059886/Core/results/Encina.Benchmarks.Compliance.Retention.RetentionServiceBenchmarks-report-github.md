```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                         | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|------:|------:|-----:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 17.47 ms |    NA |  1.00 |    9 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 17.38 ms |    NA |  1.00 |    6 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 18.89 ms |    NA |  1.08 |   11 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 17.46 ms |    NA |  1.00 |    7 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 17.34 ms |    NA |  0.99 |    4 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 17.28 ms |    NA |  0.99 |    1 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 17.30 ms |    NA |  0.99 |    3 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 17.37 ms |    NA |  0.99 |    5 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 17.47 ms |    NA |  1.00 |   10 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 17.46 ms |    NA |  1.00 |    8 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 17.29 ms |    NA |  0.99 |    2 |     576 B |        0.46 |
