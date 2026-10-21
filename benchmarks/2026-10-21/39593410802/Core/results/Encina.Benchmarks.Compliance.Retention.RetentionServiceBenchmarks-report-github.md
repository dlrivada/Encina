```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.406 μs | 1.7163 μs | 0.0941 μs |  1.00 |    0.05 |    3 | 0.0114 | 0.0076 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.268 μs | 0.2636 μs | 0.0144 μs |  0.53 |    0.02 |    2 | 0.0076 | 0.0057 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 1.821 μs | 0.1327 μs | 0.0073 μs |  0.76 |    0.03 |    3 | 0.0076 | 0.0038 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1.720 μs | 0.2546 μs | 0.0140 μs |  0.72 |    0.03 |    3 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2.687 μs | 1.5843 μs | 0.0868 μs |  1.12 |    0.05 |    3 | 0.0114 | 0.0076 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1.626 μs | 0.4567 μs | 0.0250 μs |  0.68 |    0.03 |    3 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1.611 μs | 0.2911 μs | 0.0160 μs |  0.67 |    0.02 |    3 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1.638 μs | 0.5341 μs | 0.0293 μs |  0.68 |    0.03 |    3 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 2.057 μs | 0.2686 μs | 0.0147 μs |  0.86 |    0.03 |    3 | 0.0114 | 0.0076 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1.717 μs | 0.4562 μs | 0.0250 μs |  0.71 |    0.03 |    3 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     | 1.018 μs | 0.3280 μs | 0.0180 μs |  0.42 |    0.02 |    1 | 0.0057 | 0.0038 |     576 B |        0.46 |
