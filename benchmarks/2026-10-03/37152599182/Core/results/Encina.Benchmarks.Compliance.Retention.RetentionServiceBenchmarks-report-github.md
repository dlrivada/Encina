```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2,030.9 ns |    76.62 ns |  4.20 ns |  1.00 |    0.00 |    2 | 0.0134 | 0.0114 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   946.9 ns |    42.39 ns |  2.32 ns |  0.47 |    0.00 |    1 | 0.0076 | 0.0067 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,612.2 ns |    92.11 ns |  5.05 ns |  0.79 |    0.00 |    2 | 0.0095 | 0.0076 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,377.8 ns |    57.96 ns |  3.18 ns |  0.68 |    0.00 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,233.3 ns | 1,102.69 ns | 60.44 ns |  1.10 |    0.03 |    2 | 0.0114 | 0.0076 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,328.7 ns |    67.24 ns |  3.69 ns |  0.65 |    0.00 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,303.7 ns |   174.91 ns |  9.59 ns |  0.64 |    0.00 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,278.8 ns |   120.45 ns |  6.60 ns |  0.63 |    0.00 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,717.1 ns |   215.47 ns | 11.81 ns |  0.85 |    0.01 |    2 | 0.0114 | 0.0095 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,406.1 ns |    94.38 ns |  5.17 ns |  0.69 |    0.00 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   856.5 ns |    20.20 ns |  1.11 ns |  0.42 |    0.00 |    1 | 0.0067 | 0.0057 |     592 B |        0.47 |
