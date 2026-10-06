```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  22.50 μs |  29.933 μs |  1.641 μs |  1.00 |    0.09 |    2 |  0.1221 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 102.32 μs |  99.276 μs |  5.442 μs |  4.57 |    0.36 |    4 |  0.6104 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 441.97 μs | 184.216 μs | 10.097 μs | 19.72 |    1.34 |    6 | 55.1758 | 55.1758 | 55.1758 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  12.76 μs |   3.427 μs |  0.188 μs |  0.57 |    0.04 |    1 |  0.6714 |  0.0305 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  57.72 μs |  45.507 μs |  2.494 μs |  2.58 |    0.19 |    3 |  2.8076 |  0.4883 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 283.23 μs |  93.325 μs |  5.115 μs | 12.64 |    0.85 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
