```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error     | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|----------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2,446.8 ns |  56.82 ns |  3.11 ns |  1.00 |    2 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   861.4 ns |  53.52 ns |  2.93 ns |  0.35 |    1 | 0.0391 | 0.0191 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,844.2 ns |  33.91 ns |  1.86 ns |  0.75 |    2 | 0.0515 | 0.0248 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,634.9 ns | 260.42 ns | 14.27 ns |  0.67 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,854.5 ns | 118.13 ns |  6.47 ns |  1.17 |    2 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,640.9 ns |  65.41 ns |  3.59 ns |  0.67 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,632.0 ns | 507.23 ns | 27.80 ns |  0.67 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,608.3 ns | 158.48 ns |  8.69 ns |  0.66 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2,081.7 ns | 196.47 ns | 10.77 ns |  0.85 |    2 | 0.0572 | 0.0267 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,660.2 ns | 276.87 ns | 15.18 ns |  0.68 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   806.1 ns |  90.58 ns |  4.96 ns |  0.33 |    1 | 0.0343 | 0.0172 |     592 B |        0.47 |
