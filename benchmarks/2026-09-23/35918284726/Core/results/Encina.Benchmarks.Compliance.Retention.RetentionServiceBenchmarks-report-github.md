```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2,071.9 ns |   265.34 ns | 14.54 ns |  1.00 |    0.01 |    3 | 0.0134 | 0.0114 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   937.5 ns |    43.93 ns |  2.41 ns |  0.45 |    0.00 |    1 | 0.0076 | 0.0067 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,614.7 ns |   110.37 ns |  6.05 ns |  0.78 |    0.01 |    2 | 0.0095 | 0.0076 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,350.1 ns |     7.70 ns |  0.42 ns |  0.65 |    0.00 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,198.2 ns | 1,166.61 ns | 63.95 ns |  1.06 |    0.03 |    3 | 0.0114 | 0.0076 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,314.0 ns |   208.36 ns | 11.42 ns |  0.63 |    0.01 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,305.5 ns |    47.72 ns |  2.62 ns |  0.63 |    0.00 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,274.8 ns |   119.66 ns |  6.56 ns |  0.62 |    0.00 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,694.0 ns |    92.60 ns |  5.08 ns |  0.82 |    0.01 |    2 | 0.0114 | 0.0095 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,429.2 ns |   132.23 ns |  7.25 ns |  0.69 |    0.01 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   859.1 ns |    19.93 ns |  1.09 ns |  0.41 |    0.00 |    1 | 0.0067 | 0.0057 |     592 B |        0.47 |
