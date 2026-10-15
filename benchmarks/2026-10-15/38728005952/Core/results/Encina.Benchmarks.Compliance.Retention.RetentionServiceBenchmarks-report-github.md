```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 1,988.1 ns | 680.51 ns | 37.30 ns |  1.00 |    0.02 |    2 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   702.1 ns | 168.45 ns |  9.23 ns |  0.35 |    0.01 |    1 | 0.0391 | 0.0191 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,606.4 ns |  83.49 ns |  4.58 ns |  0.81 |    0.01 |    2 | 0.0515 | 0.0248 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,435.3 ns | 312.44 ns | 17.13 ns |  0.72 |    0.01 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,195.6 ns | 250.43 ns | 13.73 ns |  1.10 |    0.02 |    2 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,403.1 ns | 138.00 ns |  7.56 ns |  0.71 |    0.01 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,366.5 ns | 424.91 ns | 23.29 ns |  0.69 |    0.02 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,457.6 ns | 241.61 ns | 13.24 ns |  0.73 |    0.01 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,696.7 ns | 848.09 ns | 46.49 ns |  0.85 |    0.02 |    2 | 0.0572 | 0.0286 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,447.0 ns | 507.10 ns | 27.80 ns |  0.73 |    0.02 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   619.2 ns | 254.09 ns | 13.93 ns |  0.31 |    0.01 |    1 | 0.0343 | 0.0172 |     592 B |        0.47 |
