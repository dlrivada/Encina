```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  36.51 μs |   1.358 μs | 0.074 μs |  1.00 |    0.00 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 178.30 μs |  12.532 μs | 0.687 μs |  4.88 |    0.02 |    4 |  3.1738 |       - |       - |  55.67 KB |        4.62 |
| &#39;JSON export: 200 activities&#39; | 781.13 μs |  48.407 μs | 2.653 μs | 21.39 |    0.07 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  14.76 μs |   0.955 μs | 0.052 μs |  0.40 |    0.00 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  66.80 μs |   4.674 μs | 0.256 μs |  1.83 |    0.01 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 397.87 μs | 127.030 μs | 6.963 μs | 10.90 |    0.17 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
