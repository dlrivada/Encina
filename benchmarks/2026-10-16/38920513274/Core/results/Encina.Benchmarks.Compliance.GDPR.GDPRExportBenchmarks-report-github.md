```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  35.33 μs |  1.953 μs | 0.107 μs |  1.00 |    0.00 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 174.40 μs |  1.871 μs | 0.103 μs |  4.94 |    0.01 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 783.14 μs | 31.474 μs | 1.725 μs | 22.16 |    0.07 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  14.84 μs |  3.894 μs | 0.213 μs |  0.42 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  67.12 μs | 17.715 μs | 0.971 μs |  1.90 |    0.02 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 406.83 μs | 69.416 μs | 3.805 μs | 11.51 |    0.10 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
