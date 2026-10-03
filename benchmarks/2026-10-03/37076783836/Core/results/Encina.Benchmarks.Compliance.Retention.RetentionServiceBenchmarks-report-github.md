```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2,114.2 ns |   589.3 ns | 32.30 ns |  1.00 |    0.02 |    2 | 0.0134 | 0.0114 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   998.2 ns |   711.6 ns | 39.00 ns |  0.47 |    0.02 |    1 | 0.0076 | 0.0057 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 1,734.2 ns |   412.7 ns | 22.62 ns |  0.82 |    0.01 |    2 | 0.0095 | 0.0076 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,428.0 ns |   275.2 ns | 15.08 ns |  0.68 |    0.01 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,412.8 ns | 1,529.4 ns | 83.83 ns |  1.14 |    0.04 |    2 | 0.0114 | 0.0076 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,409.3 ns |   581.9 ns | 31.90 ns |  0.67 |    0.02 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,393.1 ns |   626.2 ns | 34.32 ns |  0.66 |    0.02 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,393.6 ns |   663.7 ns | 36.38 ns |  0.66 |    0.02 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,921.6 ns |   327.5 ns | 17.95 ns |  0.91 |    0.01 |    2 | 0.0114 | 0.0095 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,590.8 ns |   806.9 ns | 44.23 ns |  0.75 |    0.02 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   956.9 ns |   427.4 ns | 23.43 ns |  0.45 |    0.01 |    1 | 0.0067 | 0.0057 |     592 B |        0.47 |
