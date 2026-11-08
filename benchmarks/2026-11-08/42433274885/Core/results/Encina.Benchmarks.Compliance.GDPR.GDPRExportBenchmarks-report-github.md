```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean       | Error     | StdDev     | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |-----------:|----------:|-----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  18.174 μs | 0.2138 μs |  0.3200 μs |  1.00 |    0.02 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  83.692 μs | 1.4253 μs |  2.0441 μs |  4.61 |    0.14 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 400.596 μs | 5.9261 μs |  8.6865 μs | 22.05 |    0.61 |    6 | 55.1758 | 55.1758 | 55.1758 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |   7.691 μs | 0.2240 μs |  0.3141 μs |  0.42 |    0.02 |    1 |  3.3569 |  0.2365 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  34.878 μs | 1.3304 μs |  1.9502 μs |  1.92 |    0.11 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 238.830 μs | 6.8491 μs | 10.0393 μs | 13.15 |    0.59 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
