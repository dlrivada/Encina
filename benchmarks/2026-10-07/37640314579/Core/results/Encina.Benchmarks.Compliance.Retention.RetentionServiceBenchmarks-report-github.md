```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2,058.0 ns | 1,484.8 ns |  81.39 ns |  1.00 |    0.05 |    2 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   665.1 ns |   355.5 ns |  19.48 ns |  0.32 |    0.01 |    1 | 0.0391 | 0.0191 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,725.1 ns | 1,082.1 ns |  59.31 ns |  0.84 |    0.04 |    2 | 0.0515 | 0.0248 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,545.0 ns | 1,890.7 ns | 103.63 ns |  0.75 |    0.05 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,418.7 ns | 1,714.6 ns |  93.98 ns |  1.18 |    0.06 |    2 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,515.4 ns |   527.1 ns |  28.89 ns |  0.74 |    0.03 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,518.3 ns | 1,150.0 ns |  63.04 ns |  0.74 |    0.04 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,454.1 ns |   117.5 ns |   6.44 ns |  0.71 |    0.02 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,884.8 ns |   577.1 ns |  31.63 ns |  0.92 |    0.03 |    2 | 0.0572 | 0.0286 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,629.2 ns |   579.1 ns |  31.74 ns |  0.79 |    0.03 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   698.8 ns |   211.4 ns |  11.59 ns |  0.34 |    0.01 |    1 | 0.0343 | 0.0172 |     592 B |        0.47 |
