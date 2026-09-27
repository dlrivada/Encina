```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean      | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|---------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  20.85 μs | 0.302 μs |  0.403 μs |  1.00 |    0.03 |    2 |  0.1221 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  97.37 μs | 0.385 μs |  0.552 μs |  4.67 |    0.09 |    4 |  0.6104 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 446.94 μs | 7.550 μs | 11.301 μs | 21.45 |    0.66 |    6 | 55.1758 | 55.1758 | 55.1758 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  11.17 μs | 0.270 μs |  0.369 μs |  0.54 |    0.02 |    1 |  0.6714 |  0.0305 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  48.76 μs | 0.926 μs |  1.328 μs |  2.34 |    0.08 |    3 |  2.8076 |  0.4883 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 278.23 μs | 2.904 μs |  3.876 μs | 13.35 |    0.31 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
