```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2,019.4 ns |  76.08 ns |  4.17 ns |  1.00 |    0.00 |    2 | 0.0134 | 0.0114 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   942.1 ns | 127.18 ns |  6.97 ns |  0.47 |    0.00 |    1 | 0.0076 | 0.0067 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,623.6 ns | 227.43 ns | 12.47 ns |  0.80 |    0.01 |    2 | 0.0095 | 0.0076 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,383.7 ns | 180.77 ns |  9.91 ns |  0.69 |    0.00 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,245.9 ns | 833.19 ns | 45.67 ns |  1.11 |    0.02 |    2 | 0.0114 | 0.0076 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,332.6 ns |  73.65 ns |  4.04 ns |  0.66 |    0.00 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,302.5 ns | 112.48 ns |  6.17 ns |  0.64 |    0.00 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,288.2 ns |  92.55 ns |  5.07 ns |  0.64 |    0.00 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,715.9 ns | 126.56 ns |  6.94 ns |  0.85 |    0.00 |    2 | 0.0114 | 0.0095 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,419.8 ns | 118.51 ns |  6.50 ns |  0.70 |    0.00 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   863.0 ns |  56.61 ns |  3.10 ns |  0.43 |    0.00 |    1 | 0.0067 | 0.0057 |     592 B |        0.47 |
