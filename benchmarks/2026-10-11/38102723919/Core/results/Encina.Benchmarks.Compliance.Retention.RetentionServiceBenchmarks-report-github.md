```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                         | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2,576.2 ns | 40.18 ns | 58.90 ns |  1.00 |    0.03 |    5 | 0.0725 | 0.0343 |    1312 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   954.5 ns | 31.82 ns | 47.63 ns |  0.37 |    0.02 |    2 | 0.0381 | 0.0191 |     688 B |        0.52 |
| &#39;Policy: get by ID&#39;                            | 1,933.0 ns | 35.24 ns | 51.65 ns |  0.75 |    0.03 |    3 | 0.0515 | 0.0248 |     896 B |        0.68 |
| &#39;Policy: deactivate&#39;                           | 1,817.4 ns | 31.05 ns | 45.52 ns |  0.71 |    0.02 |    3 | 0.0401 | 0.0191 |     712 B |        0.54 |
| &#39;Record: track entity&#39;                         | 3,009.1 ns | 30.92 ns | 45.32 ns |  1.17 |    0.03 |    6 | 0.0648 | 0.0305 |    1152 B |        0.88 |
| &#39;Record: mark expired&#39;                         | 1,769.3 ns | 28.74 ns | 42.12 ns |  0.69 |    0.02 |    3 | 0.0362 | 0.0172 |     640 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,810.3 ns | 18.52 ns | 27.73 ns |  0.70 |    0.02 |    3 | 0.0362 | 0.0172 |     640 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,775.5 ns | 17.98 ns | 26.91 ns |  0.69 |    0.02 |    3 | 0.0362 | 0.0172 |     640 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2,255.7 ns | 39.23 ns | 57.50 ns |  0.88 |    0.03 |    4 | 0.0572 | 0.0267 |    1024 B |        0.78 |
| &#39;Legal hold: lift&#39;                             | 1,849.2 ns | 37.78 ns | 56.55 ns |  0.72 |    0.03 |    3 | 0.0401 | 0.0191 |     712 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   840.1 ns | 17.26 ns | 23.63 ns |  0.33 |    0.01 |    1 | 0.0343 | 0.0172 |     592 B |        0.45 |
