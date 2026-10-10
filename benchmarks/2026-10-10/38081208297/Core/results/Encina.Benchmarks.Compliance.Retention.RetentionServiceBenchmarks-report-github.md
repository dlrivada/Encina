```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2,055.2 ns | 1,212.4 ns | 66.45 ns |  1.00 |    0.04 |    2 | 0.0114 | 0.0076 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1,147.3 ns |   197.7 ns | 10.84 ns |  0.56 |    0.02 |    2 | 0.0076 | 0.0057 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 1,933.2 ns |   552.6 ns | 30.29 ns |  0.94 |    0.03 |    2 | 0.0095 | 0.0076 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,470.5 ns |   283.2 ns | 15.52 ns |  0.72 |    0.02 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,336.1 ns |   357.6 ns | 19.60 ns |  1.14 |    0.03 |    2 | 0.0114 | 0.0076 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,348.4 ns |   144.8 ns |  7.94 ns |  0.66 |    0.02 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,346.9 ns |   306.9 ns | 16.82 ns |  0.66 |    0.02 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,202.9 ns |   356.8 ns | 19.56 ns |  0.59 |    0.02 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,677.7 ns |   396.5 ns | 21.73 ns |  0.82 |    0.03 |    2 | 0.0114 | 0.0095 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,352.1 ns |   299.0 ns | 16.39 ns |  0.66 |    0.02 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   863.3 ns |   165.9 ns |  9.09 ns |  0.42 |    0.01 |    1 | 0.0067 | 0.0057 |     592 B |        0.47 |
