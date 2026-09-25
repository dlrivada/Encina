```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2,620.6 ns |   470.3 ns | 25.78 ns |  1.00 |    0.01 |    3 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   922.5 ns |   337.7 ns | 18.51 ns |  0.35 |    0.01 |    1 | 0.0381 | 0.0191 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 1,889.4 ns |   303.8 ns | 16.65 ns |  0.72 |    0.01 |    2 | 0.0496 | 0.0229 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,713.7 ns |   257.4 ns | 14.11 ns |  0.65 |    0.01 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3,177.9 ns | 1,734.2 ns | 95.06 ns |  1.21 |    0.03 |    4 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,713.1 ns |   492.8 ns | 27.01 ns |  0.65 |    0.01 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,684.8 ns |   235.8 ns | 12.92 ns |  0.64 |    0.01 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,659.2 ns |   524.0 ns | 28.72 ns |  0.63 |    0.01 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2,177.6 ns |   520.2 ns | 28.52 ns |  0.83 |    0.01 |    2 | 0.0572 | 0.0267 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,779.5 ns |   738.9 ns | 40.50 ns |  0.68 |    0.01 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   898.4 ns |   122.7 ns |  6.73 ns |  0.34 |    0.00 |    1 | 0.0343 | 0.0172 |     592 B |        0.47 |
