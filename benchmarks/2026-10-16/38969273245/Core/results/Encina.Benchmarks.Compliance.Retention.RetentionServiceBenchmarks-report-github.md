```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 1,936.9 ns | 1,823.8 ns | 99.97 ns |  1.00 |    0.06 |    3 | 0.0114 | 0.0076 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1,036.5 ns |   422.0 ns | 23.13 ns |  0.54 |    0.03 |    1 | 0.0076 | 0.0057 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 1,785.3 ns |   213.7 ns | 11.71 ns |  0.92 |    0.04 |    3 | 0.0095 | 0.0076 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,405.3 ns | 1,235.8 ns | 67.74 ns |  0.73 |    0.05 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,241.1 ns |   899.1 ns | 49.28 ns |  1.16 |    0.06 |    3 | 0.0114 | 0.0076 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,303.4 ns |   184.2 ns | 10.10 ns |  0.67 |    0.03 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,261.7 ns |   205.5 ns | 11.26 ns |  0.65 |    0.03 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,297.0 ns | 1,360.1 ns | 74.55 ns |  0.67 |    0.05 |    2 | 0.0057 | 0.0038 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,829.6 ns |   723.0 ns | 39.63 ns |  0.95 |    0.05 |    3 | 0.0114 | 0.0095 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,340.0 ns |   938.3 ns | 51.43 ns |  0.69 |    0.04 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   916.9 ns |   983.1 ns | 53.89 ns |  0.47 |    0.03 |    1 | 0.0067 | 0.0057 |     592 B |        0.47 |
