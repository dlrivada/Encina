```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean       | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |-----------:|-----------:|-----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  18.250 μs |  0.2628 μs |  0.3598 μs |  1.00 |    0.03 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  85.119 μs |  1.6869 μs |  2.4727 μs |  4.67 |    0.16 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 416.224 μs |  5.1828 μs |  7.4330 μs | 22.81 |    0.59 |    6 | 55.1758 | 55.1758 | 55.1758 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |   8.506 μs |  0.2544 μs |  0.3729 μs |  0.47 |    0.02 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  36.325 μs |  0.7055 μs |  1.0560 μs |  1.99 |    0.07 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 262.034 μs | 14.9196 μs | 21.8690 μs | 14.36 |    1.21 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
