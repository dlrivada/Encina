```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                         | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 1,963.3 ns | 64.06 ns | 95.88 ns | 1,968.9 ns |  1.00 |    0.07 |    3 | 0.0744 | 0.0362 |    1280 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   629.2 ns | 46.33 ns | 67.92 ns |   584.2 ns |  0.32 |    0.04 |    1 | 0.0391 | 0.0191 |     672 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 1,516.6 ns | 20.15 ns | 29.53 ns | 1,508.4 ns |  0.77 |    0.04 |    2 | 0.0515 | 0.0248 |     896 B |        0.70 |
| &#39;Policy: deactivate&#39;                           | 1,351.3 ns | 25.31 ns | 37.11 ns | 1,339.1 ns |  0.69 |    0.04 |    2 | 0.0401 | 0.0191 |     712 B |        0.56 |
| &#39;Record: track entity&#39;                         | 2,161.1 ns | 37.75 ns | 54.13 ns | 2,150.8 ns |  1.10 |    0.06 |    4 | 0.0648 | 0.0305 |    1152 B |        0.90 |
| &#39;Record: mark expired&#39;                         | 1,422.6 ns | 22.23 ns | 32.59 ns | 1,421.7 ns |  0.73 |    0.04 |    2 | 0.0362 | 0.0172 |     640 B |        0.50 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,392.4 ns | 25.14 ns | 33.56 ns | 1,391.5 ns |  0.71 |    0.04 |    2 | 0.0362 | 0.0172 |     640 B |        0.50 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,323.7 ns | 14.57 ns | 20.42 ns | 1,321.9 ns |  0.68 |    0.03 |    2 | 0.0362 | 0.0172 |     640 B |        0.50 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,520.7 ns | 29.69 ns | 41.62 ns | 1,500.7 ns |  0.78 |    0.04 |    2 | 0.0572 | 0.0286 |     992 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 1,460.3 ns | 43.74 ns | 65.47 ns | 1,456.8 ns |  0.75 |    0.05 |    2 | 0.0401 | 0.0191 |     712 B |        0.56 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   586.7 ns | 18.84 ns | 25.78 ns |   583.0 ns |  0.30 |    0.02 |    1 | 0.0343 | 0.0172 |     592 B |        0.46 |
