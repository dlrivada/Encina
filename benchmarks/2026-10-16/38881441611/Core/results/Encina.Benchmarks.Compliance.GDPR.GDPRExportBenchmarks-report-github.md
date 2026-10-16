```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean       | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |-----------:|-----------:|-----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  18.374 μs |   3.230 μs |  0.1770 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  85.105 μs |  11.732 μs |  0.6431 μs |  4.63 |    0.05 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 422.815 μs | 170.090 μs |  9.3232 μs | 23.01 |    0.48 |    6 | 52.2461 | 52.2461 | 52.2461 | 219.51 KB |       18.21 |
| &#39;CSV export: 10 activities&#39;   |   7.914 μs |   1.525 μs |  0.0836 μs |  0.43 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  36.167 μs |  30.830 μs |  1.6899 μs |  1.97 |    0.08 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 248.812 μs | 198.231 μs | 10.8657 μs | 13.54 |    0.52 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
