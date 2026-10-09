```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 1,933.0 ns |   882.2 ns | 48.36 ns |  1.00 |    0.03 |    2 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   674.6 ns |   195.4 ns | 10.71 ns |  0.35 |    0.01 |    1 | 0.0391 | 0.0191 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,742.4 ns |   437.4 ns | 23.98 ns |  0.90 |    0.02 |    2 | 0.0515 | 0.0248 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,422.7 ns |   449.3 ns | 24.63 ns |  0.74 |    0.02 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,311.3 ns |   396.0 ns | 21.71 ns |  1.20 |    0.03 |    2 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,473.0 ns |   873.9 ns | 47.90 ns |  0.76 |    0.03 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,381.5 ns |   169.5 ns |  9.29 ns |  0.71 |    0.02 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,403.5 ns | 1,260.5 ns | 69.09 ns |  0.73 |    0.03 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,676.4 ns |   245.5 ns | 13.46 ns |  0.87 |    0.02 |    2 | 0.0572 | 0.0286 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,371.0 ns |   130.9 ns |  7.18 ns |  0.71 |    0.02 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   585.7 ns |   790.1 ns | 43.31 ns |  0.30 |    0.02 |    1 | 0.0343 | 0.0172 |     592 B |        0.47 |
