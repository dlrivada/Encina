```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean       | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |-----------:|-----------:|-----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  19.090 μs |  13.544 μs |  0.7424 μs |  1.00 |    0.05 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  86.335 μs |  13.616 μs |  0.7464 μs |  4.53 |    0.15 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 429.988 μs |  28.506 μs |  1.5625 μs | 22.55 |    0.75 |    6 | 55.1758 | 55.1758 | 55.1758 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |   8.936 μs |   9.480 μs |  0.5196 μs |  0.47 |    0.03 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  37.918 μs |   5.555 μs |  0.3045 μs |  1.99 |    0.07 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 287.028 μs | 223.455 μs | 12.2483 μs | 15.05 |    0.75 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
