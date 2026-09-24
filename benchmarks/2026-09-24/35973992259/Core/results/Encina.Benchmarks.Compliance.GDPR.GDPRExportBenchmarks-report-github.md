```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  31.65 μs |   5.567 μs |  0.305 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.03 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 147.81 μs |  11.997 μs |  0.658 μs |  4.67 |    0.04 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.64 |
| &#39;JSON export: 200 activities&#39; | 711.34 μs | 191.124 μs | 10.476 μs | 22.48 |    0.34 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.28 |
| &#39;CSV export: 10 activities&#39;   |  14.08 μs |   2.374 μs |  0.130 μs |  0.45 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  63.60 μs |   5.283 μs |  0.290 μs |  2.01 |    0.02 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.37 |
| &#39;CSV export: 200 activities&#39;  | 398.54 μs | 180.312 μs |  9.883 μs | 12.59 |    0.29 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.84 |
