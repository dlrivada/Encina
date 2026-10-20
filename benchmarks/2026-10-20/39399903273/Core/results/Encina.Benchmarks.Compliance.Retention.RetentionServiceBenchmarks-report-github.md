```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2,068.9 ns |   600.54 ns | 32.92 ns |  1.00 |    0.02 |    2 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   710.7 ns |   627.03 ns | 34.37 ns |  0.34 |    0.02 |    1 | 0.0391 | 0.0191 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,696.5 ns |    35.19 ns |  1.93 ns |  0.82 |    0.01 |    2 | 0.0515 | 0.0248 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,495.9 ns |   419.31 ns | 22.98 ns |  0.72 |    0.01 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,481.7 ns |   733.25 ns | 40.19 ns |  1.20 |    0.02 |    2 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,625.3 ns |   770.50 ns | 42.23 ns |  0.79 |    0.02 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,570.4 ns |   229.78 ns | 12.59 ns |  0.76 |    0.01 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,544.5 ns |   366.10 ns | 20.07 ns |  0.75 |    0.01 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,915.8 ns |   935.92 ns | 51.30 ns |  0.93 |    0.03 |    2 | 0.0572 | 0.0286 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,637.4 ns | 1,504.82 ns | 82.48 ns |  0.79 |    0.04 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   710.2 ns |    69.91 ns |  3.83 ns |  0.34 |    0.01 |    1 | 0.0343 | 0.0172 |     592 B |        0.47 |
